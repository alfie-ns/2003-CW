from django.db import models

# response/views.py
from rest_framework import viewsets
from rest_framework.decorators import action
from rest_framework.response import Response
from .models import GameSession, AIResponse
from .services import OpenAIService
from .serializers import GameSessionSerializer, AIResponseSerializer

class GameSessionViewSet(viewsets.ModelViewSet):
    queryset = GameSession.objects.all()
    serializer_class = GameSessionSerializer
    
    @action(detail=True, methods=['post'])
    def ai_response(self, request, pk=None):
        session = self.get_object()
        prompt = request.data.get('prompt')
        
        ai_service = OpenAIService()
        response = ai_service.get_ai_response(prompt, session.game_state)
        
        ai_response = AIResponse.objects.create(
            session=session,
            prompt=prompt,
            response=response
        )
        
        return Response({
            'response': response,
            'session_id': session.session_id
        })

