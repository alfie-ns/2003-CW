from rest_framework.response import Response
from rest_framework.views import APIView
from rest_framework.exceptions import NotFound
from rest_framework import status
from .models import GameSession, AIResponse
from .serialisers import GameSessionSerialiser, AIResponseSerialiser
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

# Extract OpenAI key; ensure it's set in the environment variables
openai.api_key = config("OPENAI_API_KEY")

class AIResponseView(APIView):

    """
    docs/API.md for full API documentation.

    GET    – retrieve a session’s details.
    POST   – process a prompt via OpenAI and update game state.
    PUT    – update the game state of an existing session.
    DELETE – remove a session (and its AIResponses) from the database.
    """ 
  
    def get(self, request, pk=None):
        session = self.get_object(pk)  # fetch the session object
        serialiser = GameSessionSerialiser(session) # serialise the session object
        return Response(serialiser.data) # return the serialised data i.e. session_id, created_at, and game_state

    def post(self, request, pk=None):
        # Retrieve the session object
        try:
            session = GameSession.objects.get(pk=pk)
        except GameSession.DoesNotExist:
            # still proceed with a normal request; store the session ID in the sqlite database 
            session = GameSession.objects.create(session_id=pk, game_state=request.data.get("game_state", {})) 
            
        # Extract the prompt from the request
        prompt = request.data.get("prompt")
        if not prompt:
            return Response({"error": "Prompt is required"}, status=status.HTTP_400_BAD_REQUEST) # if Unity doesn't send a prompt, respond with clear error message
        
        # Extract the game_state and player_balance from the Unity -> Django request
        game_state = request.data.get("game_state", {})
        player_balance = game_state.get("score", 0)  # fetch score from game_state
    
        if game_state: # if game state is provided, synchronise it, save to database
            session.game_state = game_state
            session.save()

        # Enhance the prompt's context with constructed player balance info
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
        response.save() # save to database
        
        # Serialise the response
        response_serialiser = AIResponseSerialiser(response).data
        session_serialiser = GameSessionSerialiser(session).data
        print(f"Response Serialiser:\n {response_serialiser}\n")
        print(f"Session Serialiser:\n {session_serialiser}")



        # Return the serialised data
        return Response({
            "message": response_serialiser['response'],  # get 'response' text from serialised AIResponse data
            "metadata": {
                "session_id": session_serialiser['session_id'],    
                "timestamp": response_serialiser['timestamp'], 
                "game_state": session_serialiser['game_state'], 
            }
        }, status=status.HTTP_201_CREATED)
    
    def put(self, request, pk=None):
        # Update the game state of an existing session
        try:
            session = GameSession.objects.get(pk=pk)
        except GameSession.DoesNotExist:
            raise NotFound("GameSession not found")

        game_state = request.data.get("game_state")
        if game_state is None:
            return Response(
                {"error": "game_state field is required"},
                status=status.HTTP_400_BAD_REQUEST
            )

        session.game_state = game_state
        session.save()

        serialised = GameSessionSerialiser(session).data
        return Response(serialised, status=status.HTTP_200_OK)
    
    def delete(self, request, pk=None):
        # Delete a session and cascade to AIResponses
        try:
            session = GameSession.objects.get(pk=pk)
        except GameSession.DoesNotExist:
            raise NotFound("GameSession not found")

        session.delete()  # cascades to AIResponse via FK
        return Response({"message": "Session deleted"}, status=status.HTTP_200_OK)
    
    # ---

    # Helper functions:
    def get_object(self, pk):
        try:
            return GameSession.objects.get(pk=pk)
        except GameSession.DoesNotExist:
            raise NotFound("GameSession not found")