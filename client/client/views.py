from django.http import HttpResponseRedirect
from django.shortcuts import render
from forms import SignInForm, SignUpForm
import utils


def index(request):
    return render(request, 'map.html')


# ========== U S E R   M A N A G E M E N T ==========
def sign_in(request):
    if request.user.is_authenticated():
        return HttpResponseRedirect('/')

    if request.method == 'POST':
        form = SignInForm(request.POST)
        if form.is_valid():
            result, request = utils.sign_in(request)
            if result:
                return HttpResponseRedirect('/')
            else:
                return render(request, 'general_form.html', {'form_id': 'sign-in',
                                                             'form_submit': 'sign in',
                                                             'form': form})
        else:
            return render(request, 'general_form.html', {'form_id': 'sign-in',
                                                         'form_submit': 'sign in',
                                                         'form': form})
    else:
        form = SignInForm()
        return render(request, 'general_form.html', {'form_id': 'sign-in',
                                                     'form_submit': 'sign in',
                                                     'form': form})


def sign_out(request):
    utils.sign_out(request)
    return HttpResponseRedirect('/')


def sign_up(request):
    if request.method == 'POST':
        form = SignUpForm(request.POST)
        if form.is_valid() and request.POST['password'] == request.POST['retyped_password']:
            utils.sign_up(request)
            return HttpResponseRedirect('/')
        else:
            return render(request, 'general_form.html', {'form_id': 'sign-up',
                                                         'form_submit': 'sign up',
                                                         'form': form})
    else:
        form = SignUpForm()
        return render(request, 'general_form.html', {'form_id': 'sign-up',
                                                     'form_submit': 'sign up',
                                                     'form': form})


# ========== E X T R A S ==========
def create_exemplary_data(request):
    utils.create_exemplary_data()
    return HttpResponseRedirect('/')