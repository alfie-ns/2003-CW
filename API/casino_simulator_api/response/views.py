from rest_framework.response import Response
from rest_framework.views import APIView
from rest_framework.exceptions import NotFound
from rest_framework import status
from .models import GameSession, AIResponse
from .serializers import GameSessionSerializer, AIResponseSerializer
from django.utils.timezone import localtime
from uuid import UUID
from decouple import config
import openai

system_message = '''
You are the charismatic AI host of the Casino Simulator. Your goal is to create an engaging, realistic casino experience while helping players understand the games.

Respond like this exactly, concisely: "
1- Acknowledge their current action or bet with appropriate casino atmosphere
2- Explain the outcome of their bet clearly (wins, losses)
3- Provide helpful context about their current standing (chips, streak, etc.)
4- Suggest possible next moves based on their situation
5- Occasionally offer a brief gambling tip or strategy insight
6- Keep responses concise and lively (50-75 words maximum)

Maintain a balanced, realistic tone and celebrate wins enthusiastically; but don't overpromise future success. The experience should feel authentic to a real casino 
where the user isn't being pressured to gamble more, but rather encouraged to enjoy the game.
'''

# Extract OpenAI key; ensure it's set in the environment variables file
openai.api_key = config("OPENAI_API_KEY")

class AIResponseView(APIView):
    def get_object(self, pk):
        try:
            return GameSession.objects.get(pk=pk)
        except GameSession.DoesNotExist:
            raise NotFound("GameSession not found")

    def post(self, request, pk=None):
        print("Request received with pk:", pk)
        print("Request data:", request.data)

        # Retrieve the session object
        try:
            session = GameSession.objects.get(pk=pk)
        except GameSession.DoesNotExist:
            session = GameSession.objects.create(session_id=pk, game_state=request.data.get("game_state", {}))

        # Extract the prompt from the request
        prompt = request.data.get("prompt")
        if not prompt:
            return Response({"error": "Prompt is required"}, status=status.HTTP_400_BAD_REQUEST)
        
        # Get the game state and extract player balance
        game_state = request.data.get("game_state", {})
        player_balance = game_state.get("score", 0)  # Extract balance from score field; 0 if empty
    
        if game_state:
            session.game_state = game_state
            session.save()

        # Enhance the prompt with player balance info
        final_prompt = f"Player's current balance: ${player_balance}. {prompt}"

        # Call OpenAI API to generate the response
        try:
            response = openai.chat.completions.create(
                model="gpt-4.1-mini", 
                messages=[
                    {"role": "system", "content": system_message},
                    {"role": "user", "content": final_prompt}
                ]
            )
            response_text = response.choices[0].message.content  # Extract text from OpenAI response
        except Exception as e:
            return Response({"error": f"OpenAI API error: {str(e)}"}, status=status.HTTP_500_INTERNAL_SERVER_ERROR)

        # Create and save the AIResponse object
        response = AIResponse(
            session=session,
            prompt=prompt,
            response=response_text
        )
        print ("AI response data.", response.__dict__)
        response.save() # save to database
        
        # Serialise the response
        response_serialiser = AIResponseSerializer(response)
        session_serialiser = GameSessionSerializer(session)
        print(response_serialiser.data)
        print(session_serialiser.data)

        print("Final response data:", {
            "response": response_text,
            "session_id": session.session_id,
            "response_data_keys": response_serialiser.data.keys(),
            "session_data_keys": session_serialiser.data.keys() if session_serialiser.data else "None",
        })
        
        # Return the serialised data
        return Response({
            "message": response_text,
            "metadata": {
                "session_id": session.session_id,
                "timestamp": localtime(response.timestamp).isoformat(),
                "game_state": session.game_state
            }
        }, status=status.HTTP_201_CREATED)