from rest_framework import serializers
from .models import GameSession, AIResponse
from zoneinfo import ZoneInfo


'''
    This file searialises the data for the API request;

    GameSessionSerializer: Serialises the GameSession model with the following fields:
        - session_id: The unique identifier for the game session.
        - created_at: The timestamp when the session was created.
        - game_state: The current state of the game.

    AIResponseSerializer: Serialises the AIResponse model with the following fields:
        - session: The game session associated with the AI response.
        - prompt: Prompt sent to the AI.
        - response: Response received from the AI.
        - timestamp: The timestamp when the response was received.

    These fields essentially contain the data contract between the Unity front-end and Django back-end. 
    ApiManager in Unity expects to function with this exact this exact structure to deserialise the 
    response into GameState objects with player_name, score, level, and status properties. Without this precise format, the game would fail to parse responses, as evidenced by how the HandleApiResponse method processes JSON payloads to extract both the AI text and game state in a single transaction.
'''

class GameSessionSerializer(serializers.ModelSerializer):
    class Meta:
        model = GameSession
        fields = ['session_id', 'created_at', 'game_state']

class AIResponseSerializer(serializers.ModelSerializer):
    # Force British timezone
    timestamp = serializers.DateTimeField(
        format='%Y-%m-%dT%H:%M:%S%z',
        default_timezone=ZoneInfo('Europe/London'),
    )
    print(timestamp)
    class Meta:
        model = AIResponse
        fields = ['session', 'prompt', 'response', 'timestamp']