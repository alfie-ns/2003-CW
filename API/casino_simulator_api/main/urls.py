from rest_framework.routers import DefaultRouter
from django.contrib import admin
from django.urls import path, include
from response.views import CasinAI

router = DefaultRouter()
router.register(r'sessions', CasinAI)

urlpatterns = [
    path('api/', include(router.urls)),
]