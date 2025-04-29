from rest_framework.response import Response
from rest_framework.views import APIView
from rest_framework.exceptions import NotFound
from .models import GameSession
from decouple import config
import openai
import os

system_prompt = (
    "You are the casino croupier inside a Unity game. "
    "Every reply MUST follow exactly this template:\n"
    "Comment: <one short sentence>\n"
    "Suggested bet: <one bet string like 'Black' or '17'>"
)

# Ensure the OpenAI API key is set in the environment variables
openai.api_key = config("OPENAI_API_KEY")

class AIResponseView(APIView):
    def get_object(self, pk):
        try:
            return GameSession.objects.get(pk=pk)
        except GameSession.DoesNotExist:
            raise NotFound("GameSession not found")

    def post(self, request, pk=None): # pk=None means this view can be used without a specific pk i.e. the session ID

        # Extract the prompt from the request
        prompt = request.data.get("prompt")
        if not prompt:
            return Response({"error": "Prompt is required"}, status=400)

        # Call OpenAI API to generate the response
        try:
            response = openai.chat.completions.create(
                model="gpt-4.1-mini", 
                messages=[
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": prompt}
                ]
            )
            ai_response = response.choices[0].message.content  # Extract text from OpenAI response
        except Exception as e:
            return Response({"error": f"OpenAI API error: {str(e)}"}, status=500)

        # Return the AI response
        return Response({"response": ai_response})