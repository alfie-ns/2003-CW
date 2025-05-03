from rest_framework.response import Response
from rest_framework.views import APIView
from rest_framework.exceptions import NotFound
from .models import GameSession
from decouple import config
import openai

system_message = '''
You are the charismatic AI host of the Casino Simulator. Your goal is to create an engaging, realistic casino experience while helping players understand the games.

Respond like this exactly, concisely:
1. Acknowledge their current action or bet with appropriate casino atmosphere
2. Explain the outcome of their bet clearly (wins, losses, special events)
3. Provide helpful context about their current standing (chips, streak, etc.)
4. Suggest possible next moves based on their situation
5. Occasionally offer a brief gambling tip or strategy insight
6. Keep responses concise and lively (50-75 words maximum)

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
        # Retrieve the session object
        session = self.get_object(pk)

        # Extract the prompt from the request
        prompt = request.data.get("prompt")
        if not prompt:
            return Response({"error": "Prompt is required"}, status=400)

        # Call OpenAI API to generate the response
        try:
            response = openai.chat.completions.create(
                model="gpt-4.1-mini", 
                messages=[
                    {"role": "system", "content": system_message},
                    {"role": "user", "content": prompt}
                ]
            )
            ai_response = response.choices[0].message.content  # Extract text from OpenAI response
        except Exception as e:
            return Response({"error": f"OpenAI API error: {str(e)}"}, status=500)

        # Return the AI response
        return Response({
            "response": ai_response,
            "session_id": session.session_id,
            "game_state": session.game_state
        })