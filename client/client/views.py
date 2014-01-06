from django.http import HttpResponse, HttpResponseRedirect
from django.shortcuts import render
from forms import SignInForm, SignUpForm
import utils
import json


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


# ========== P O P U P S   M A N A G E M E N T ==========
def get_popup(request, popup_type='start-routing'):
    if request.method == 'POST':
        data = utils.get_popup(popup_type, request)
        if data['type'] == 'form' and data['form'].is_valid():
            result, marker_id = utils.add_marker(request)
            if result:
                return HttpResponse(content=str(marker_id), content_type='text/plain')
            else:
                return HttpResponse(content='fail', content_type='text/plain')
        else:
            return render(request, 'popup.html', {'data': data})
    else:
        data = utils.get_popup(popup_type)
        return render(request, 'popup.html', {'data': data})


def remove_marker(request):
    if utils.remove_marker(request):
        return HttpResponse(content='ok', content_type='text/plain')
    else:
        return HttpResponse(content='fail', content_type='text/plain')


# ========== S T R E E T S   G R A P H   M A N A G E M E N T ==========
def get_graph(request):
    data = {'nodes': utils.get_nodes(),
            'lines': utils.get_lines()}
    return HttpResponse(content=json.dumps(data), content_type='application/json')


# ========== M A R K E R S   M A N A G E M E N T ==========
def get_markers(request):
    data = {'markers': utils.get_markers(request)}
    return HttpResponse(content=json.dumps(data), content_type='application/json')


# ========== E X T R A S ==========
def create_exemplary_data(request):
    utils.create_exemplary_data(request)
    return HttpResponseRedirect('/')