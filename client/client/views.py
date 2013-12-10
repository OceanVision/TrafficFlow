from django.template import Context, RequestContext
from django.http import HttpResponseRedirect
from django.shortcuts import render, render_to_response
from forms import SignInForm, SignUpForm
import utils


def index(request):
    return render(request, "map.html")


def sign_in(request):
    if request.method == "POST":
        form = SignInForm(request.POST)
        if form.is_valid():
            result, request = utils.sign_in(request)
            if result:
                return HttpResponseRedirect("/")
            else:
                ctx = Context({"form_id": "sign-in", "form_submit": "sign in", "form": form})
                return render_to_response("general_form.html", ctx, context_instance=RequestContext(request))
        else:
            ctx = Context({"form_id": "sign-in", "form_submit": "sign in", "form": form})
            return render_to_response("general_form.html", ctx, context_instance=RequestContext(request))
    else:
        if request.user.is_authenticated():
            return HttpResponseRedirect("/")

        form = SignInForm()
        ctx = Context({"form_id": "sign-in", "form_submit": "sign in", "form": form})
        return render_to_response("general_form.html", ctx, context_instance=RequestContext(request))


def sign_out(request):
    utils.sign_out(request)
    return HttpResponseRedirect("/")


def sign_up(request):
    if request.method == "POST":
        form = SignUpForm(request.POST)
        if form.is_valid() and request.POST["password"] == request.POST["retyped_password"]:
            utils.sign_up(request)
            return HttpResponseRedirect("/")
        else:
            ctx = Context({"form_id": "sign-up", "form_submit": "sign up", "form": form})
            return render_to_response("general_form.html", ctx, context_instance=RequestContext(request))
    else:
        form = SignUpForm()
        ctx = Context({"form_id": "sign-up", "form_submit": "sign up", "form": form})
        return render_to_response("general_form.html", ctx, context_instance=RequestContext(request))