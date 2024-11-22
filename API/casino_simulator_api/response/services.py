import openai
from django.conf import settings

"""
Basic service to interact with OpenAI API.
"""

class OpenAIService:
    def __init__(self):
        openai.api_key = settings.OPENAI_API_KEY
        
    def get_ai_response(self, prompt, context=None):
        try:
            response = openai.ChatCompletion.create(
                model="gpt-4o-mini",
                messages=[
                    {"role": "system", "content": "You are a casino dealer assistant."},
                    {"role": "user", "content": prompt}
                ]
            )
            return response.choices[0].message.content
        except Exception as e:
            return str(e)