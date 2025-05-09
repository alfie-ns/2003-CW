from django.contrib import admin
from django.urls import path, include
from django.http import JsonResponse

def health_check(request):
    return JsonResponse({"status": "ok"})

urlpatterns = [
    path('admin/', admin.site.urls),  # Django admin interface
    path('api/', include('response.urls')), # AI response endpoint; use response/urls.py
    path('', health_check), # Health check endpoint for Render confirmation
]