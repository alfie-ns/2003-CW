from django.urls import path
from .views import AIResponseView

urlpatterns = [
    path('sessions/<uuid:pk>/response/', AIResponseView.as_view(), name='ai-response'), # path to the AIResponseView
]