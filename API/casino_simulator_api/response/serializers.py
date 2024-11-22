from rest_framework import serializers
from .models import GameSession, AIResponse

class GameSessionSerializer(serializers.ModelSerializer):
    class Meta:
        model = GameSession
        fields = ['session_id', 'created_at', 'game_state']

class AIResponseSerializer(serializers.ModelSerializer):
    class Meta:
        model = AIResponse
        fields = ['session', 'prompt', 'response', 'timestamp']