from rest_framework.response import Response
from rest_framework.views import APIView
from rest_framework.exceptions import NotFound
from .models import GameSession
from decouple import config
import openai
import os

# Ensure the OpenAI API key is set in the environment variables
openai.api_key = config("OPENAI_API_KEY")

class AIResponseView(APIView):
    def get_object(self, pk):
        try:
            return GameSession.objects.get(pk=pk)
        except GameSession.DoesNotExist:
            raise NotFound("GameSession not found")

    def post(self, request, pk=None):
        print(f"OpenAI API Key: {openai.api_key}") # debug to confirm the API key is set
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
                    {"role": "system", "content": "You are assisting with a Casino Simulator game simulation."},
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