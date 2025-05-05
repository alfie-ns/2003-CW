# API Notes

## Django Admin Login Details

```plaintext
username: oladeanio
password: root
```

Logs me in as a superuser to the admin interface, where I can manage the session IDs.

`the above login details are only pushed to the repo for coursework demonstration purposes`

---

## API Endpoints

`casino_simulator_api/main/urls.py` {

```python
def health_check(request):
    return JsonResponse({"status": "ok"})

urlpatterns = [
    path('admin/', admin.site.urls),
    path('api/sessions/<uuid:pk>/response/', AIResponseView.as_view(), name='ai-response'),
    path('', health_check),
]
```

- `path('api/sessions/<uuid:pk>/response/', AIResponseView.as_view(), name='ai-response')` a ``POST`` request i.e. Unity -> Django -> OpenAI -> Django -> Unity; gets a response from OpenAI and sends it back to Unity
- `path('admin/', admin.site.urls)` Used for accessing the Django Admin login page
- `path('', health_check)` Basic health check endpoint to confirm the server is running

The `health_check(request)` function returns a simple JSON response (status": "ok") indicating the server's status. This is used to confirm the server is running when debugging the game; the request parameter is the HTTP request object, and the function returns a `JsonResponse` with a status message.

}

`casino_simulator_api/response/urls.py` {

```python
urlpatterns = [
    path('sessions/<uuid:pk>/response/', AIResponseView.as_view(), name='ai-response'),
]
```

The urlpattern for the AIResponseView is defined here, it handles the POST request from Unity to get a response from OpenAI.
With the session ID, the AIResponseView can retrieve the session object and use it to get the game state and other relevant information.

}

## API Views

`casino_simulator_api/response/views.py` POST request {

```python
def post(self, request, pk=None):
        session = self.get_object(pk)

    prompt = request.data.get("prompt")
        if not prompt:
            return Response({"error": "Prompt is required"}, status=400)

    try:
            response = openai.chat.completions.create(
                model="gpt-4.1-mini",
                messages=[
                    {"role": "user", "content": system_message},
                    {"role": "user", "content": prompt}
                ]
            )
            response = response.choices[0].message.content
        except Exception as e:
            return Response({"error": f"OpenAI API error: {str(e)}"}, status=500)

    return Response({
            "response": response,
            "session_id": session.session_id,
        })

```

Firstly, the function verifies that the OpenAI API Key is set and accessible(debugging purposes). It then retrieves the session object using the provided `pk` (primary key) and checks if a prompt is included in the request data, if not, it returns a 400 error response. It proceeds to call the OpenAI API with the constructed prompt and the system message and returns the AI's response along with the session ID and game state in the response. If any error occurs during the OpenAI API call, it returns a 500 error response with the error message.

}

`casino_simulator_api/response/serializers.py` {

```python
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
```

The `GameSessionSerializer` and `AIResponseSerializer` classes are serialisers for the `GameSession` and `AIResponse` models, respectively. They define how these models are represented in JSON format when sent to or received from the API. The `fields` attribute specifies all the fields of the model that should be included in the serialised output.

}

## API Models

`casino_simulator_api/response/models.py` {

```python
class GameSession(models.Model):
    session_id = models.UUIDField(primary_key=True) 
    created_at = models.DateTimeField(auto_now_add=True)
    game_state = models.JSONField()

class AIResponse(models.Model):
    session = models.ForeignKey(GameSession, on_delete=models.CASCADE)
    prompt = models.TextField()
    response = models.TextField() # Essentially response.choices[0].message.content
    timestamp = models.DateTimeField(auto_now_add=True)
```

The `GameSession` model represents a game session with a unique session ID, creation timestamp, and game state stored in JSON format. The `AIResponse` model represents the AI's response to a prompt, linked to a specific game session. It includes the prompt text, the AI's response text, and a timestamp for when the response was generated.

The `ForeignKey` relationship between `AIResponse` and `GameSession` allows for tracking which AI response corresponds to which game session. The `on_delete=models.CASCADE` argument ensures that if a game session is deleted, all associated AI responses are also deleted.

The response(`models.TextField()) is essentially the response.choices[0].message.content from the OpenAI API call, which contains the AI's generated text based on the prompt and game state.

}
