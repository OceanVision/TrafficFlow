from django import forms


class SignInForm(forms.Form):
    username = forms.CharField(max_length=50, required=True, label='Username')
    password = forms.CharField(max_length=50, required=True, label='Password', widget=forms.PasswordInput())


class SignUpForm(forms.Form):
    username = forms.CharField(max_length=50, required=True, label='Username')
    password = forms.CharField(max_length=50, required=True, label='Password', widget=forms.PasswordInput())
    retyped_password = forms.CharField(max_length=50, required=True, label='Retype password', widget=forms.PasswordInput())


class AddMarkerForm(forms.Form):
    longitude = forms.CharField(widget=forms.HiddenInput(), required=True)
    latitude = forms.CharField(widget=forms.HiddenInput(), required=True)
    title = forms.CharField(max_length=50, required=False, label='Title')
    description = forms.CharField(max_length=50, required=False, label='Description')