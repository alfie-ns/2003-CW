from django.db import models

class GameSession(models.Model):
    session_id = models.UUIDField(primary_key=True)
    created_at = models.DateTimeField(auto_now_add=True)
    game_state = models.JSONField()
    
class AIResponse(models.Model):
    session = models.ForeignKey(GameSession, on_delete=models.CASCADE)
    prompt = models.TextField()
    response = models.TextField()
    timestamp = models.DateTimeField(auto_now_add=True)
