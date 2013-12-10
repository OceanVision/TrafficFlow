from django.contrib.auth import authenticate, login, logout
from django.contrib.auth.models import User


def sign_in(request):
    user = authenticate(username=request.POST["username"], password=request.POST["password"])
    if user is not None:
        if user.is_active:
            login(request, user)
            return True, request
        else:
            return False, request
    else:
        return False, request


def sign_out(request):
    if request.user.is_authenticated:
        logout(request)
        return True
    else:
        return False


def sign_up(request):
    user = User.objects.create_user(username=request.POST["username"], password=request.POST["password"])
    user.save()