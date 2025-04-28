from django.contrib import admin
from django.urls import path
from response.views import AIResponseView
from django.http import JsonResponse

def health_check(request):
    return JsonResponse({"status": "ok"})

urlpatterns = [
    path('admin/', admin.site.urls),  # Django admin interface
    path('api/sessions/<uuid:pk>/response/', AIResponseView.as_view(), name='ai-response'),  # AI response endpoint
    path('', health_check), # Health check endpoint for Render confirmation
]