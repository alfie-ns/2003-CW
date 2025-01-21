from django.contrib import admin
from .models import GameSession

@admin.register(GameSession)
class GameSessionAdmin(admin.ModelAdmin):
    list_display = ('session_id', 'created_at')  # Add 'session_id' to the list_display