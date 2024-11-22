from django.contrib import admin
from django.urls import path
from response.views import AIResponseView

urlpatterns = [
    path('admin/', admin.site.urls),  # Django admin interface
    path('api/sessions/<uuid:pk>/response/', AIResponseView.as_view(), name='ai-response'),  # AI response endpoint
]