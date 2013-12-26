from django.conf.urls import patterns, include, url
from django.contrib import admin
admin.autodiscover()
import views


urlpatterns = patterns('',
    url(r'^$', views.index, name='index'),
    url(r'^sign_in$', views.sign_in, name='sign_in'),
    url(r'^sign_out$', views.sign_out, name='sign_out'),
    url(r'^sign_up$', views.sign_up, name='sign_up'),

    url(r'^get_graph$', views.get_graph, name='get_graph'),
    url(r'^get_markers$', views.get_markers, name='get_markers'),
    url(r'^add_marker$', views.add_marker, name='add_marker'),

    url(r'^create_exemplary_data$', views.create_exemplary_data, name='create_exemplary_data'),
    url(r'^admin/', include(admin.site.urls)),
    url(r'^admin/doc/', include('django.contrib.admindocs.urls')),
)
