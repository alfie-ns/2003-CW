from rest_framework.response import Response
from rest_framework.views import APIView
from rest_framework.exceptions import NotFound
from .models import GameSession
import openai

class AIResponseView(APIView):

    def get_object(self, pk):
        try:
            return GameSession.objects.get(pk=pk)
        except GameSession.DoesNotExist:
            raise NotFound("GameSession not found")

    def ai_response(self, request, pk=None):
        # retrieve the session object
        session = self.get_object(pk)

        # extract the prompt from the request
        prompt = request.data.get('prompt')
        if not prompt:
            return Response({'error': 'Prompt is required'}, status=400)

        # call OpenAI API to generate the response
        try:
            openai_response = openai.ChatCompletion.create(
                model="gpt-4o-mini",
                messages=[
                    {"role": "system", "content": f"todo..."},
                    {"role": "user", "content": prompt}
                ]
            )
            response = openai_response.choices[0].message.content # extract text from the response
        except Exception as e:
            return Response({'error': f"OpenAI API error: {str(e)}"}, status=500)

        # return the AI response
        return Response({
            'response': response,
            'session_id': session.session_id
        })