from django.contrib.auth import authenticate, login, logout
from django.contrib.auth.models import User
from models import StreetsNode, StreetsLine, Marker
from forms import AddMarkerForm


# ========== U S E R   M A N A G E M E N T ==========
def sign_in(request):
    user = authenticate(username=request.POST['username'], password=request.POST['password'])
    if user is not None:
        if user.is_active:
            login(request, user)
            return True, request
        else:
            return False, request
    else:
        return False, request


def sign_out(request):
    if request.user.is_authenticated():
        logout(request)
        return True
    else:
        return False


def sign_up(request):
    user = User.objects.create_user(username=request.POST['username'], password=request.POST['password'])
    user.save()


# ========== P O P U P S   M A N A G E M E N T ==========
def get_popup(popup_type, request=None):
    if popup_type == 'add-marker':
        if request:
            data = {'type': 'form', 'form': AddMarkerForm(request.POST), 'form_submit': 'add marker'}
        else:
            data = {'type': 'form', 'form': AddMarkerForm(), 'form_submit': 'add marker'}
    elif popup_type == 'start-routing':
        data = {'type': 'text', 'text': 'Click here to remove marker.'}
    return data


# ========== S T R E E T S   G R A P H   M A N A G E M E N T ==========
def get_nodes():
    retrieved_nodes = StreetsNode.objects.all()
    data = []
    for node in retrieved_nodes:
        data.append({'id': node.id,
                     'longitude': node.longitude,
                     'latitude': node.latitude,
                     'title': node.title,
                     'description': node.description})
    return data


def get_lines():
    retrieved_lines = StreetsLine.objects.all()
    data = []
    for line in retrieved_lines:
        data.append({'startNodeId': line.start_node.id,
                     'endNodeId': line.end_node.id,
                     'ways': line.ways})
    return data


# ========== M A R K E R S   M A N A G E M E N T ==========
def get_markers(request):
    if not request.user.is_authenticated():
        return []

    retrieved_markers = Marker.objects.filter(user__username=request.user.username) # troche lamerskie
    data = []
    for marker in retrieved_markers:
        data.append({'id': marker.id,
                     'longitude': marker.longitude,
                     'latitude': marker.latitude,
                     'title': marker.title,
                     'description': marker.description})
    return data


def add_marker(request):
    if not request.user.is_authenticated():
        return False, 0

    longitude = request.POST['longitude']
    latitude = request.POST['latitude']
    title = request.POST['title']
    description = request.POST['description']
    marker = Marker(user=User.objects.get(username=request.user.username),
                    longitude=longitude, latitude=latitude, title=title, description=description)
    marker.save()
    return True, marker.id


def remove_marker(request):
    if not request.user.is_authenticated():
        return False

    marker_id = request.GET['id']
    Marker.objects.get(id=marker_id).delete()
    return True


# ========== E X T R A S ==========
def create_exemplary_data(request):
    if not request.user.is_authenticated() or not request.user.is_superuser:
        return False

    StreetsNode.objects.all().delete()
    Marker.objects.all().delete()

    # create streets nodes
    streets_nodes = list()
    streets_nodes.append(StreetsNode(longitude=19.90599, latitude=50.02691, title='', description=''))
    streets_nodes.append(StreetsNode(longitude=19.91291, latitude=50.02975, title='', description=''))
    streets_nodes.append(StreetsNode(longitude=19.91295, latitude=50.02976, title='', description=''))
    streets_nodes.append(StreetsNode(longitude=19.91471, latitude=50.02806, title='', description=''))
    streets_nodes.append(StreetsNode(longitude=19.91474, latitude=50.02808, title='', description=''))
    streets_nodes.append(StreetsNode(longitude=19.91449, latitude=50.02798, title='', description=''))
    streets_nodes.append(StreetsNode(longitude=19.91316, latitude=50.02740, title='', description=''))
    streets_nodes.append(StreetsNode(longitude=19.91288, latitude=50.02719, title='', description=''))
    streets_nodes.append(StreetsNode(longitude=19.91276, latitude=50.02703, title='', description=''))
    streets_nodes.append(StreetsNode(longitude=19.91272, latitude=50.02690, title='', description=''))
    streets_nodes.append(StreetsNode(longitude=19.91270, latitude=50.02671, title='', description=''))

    for node in streets_nodes:
        node.save()

    # create streets lines
    streets_lines = list()
    streets_lines.append(StreetsLine(start_node=StreetsNode.objects.get(id=1), end_node=StreetsNode.objects.get(id=2), ways=2))
    # streets_lines.append(StreetsLine(start_node=StreetsNode.objects.get(id=2), end_node=StreetsNode.objects.get(id=3)))
    streets_lines.append(StreetsLine(start_node=StreetsNode.objects.get(id=2), end_node=StreetsNode.objects.get(id=4)))
    streets_lines.append(StreetsLine(start_node=StreetsNode.objects.get(id=3), end_node=StreetsNode.objects.get(id=5)))
    streets_lines.append(StreetsLine(start_node=StreetsNode.objects.get(id=4), end_node=StreetsNode.objects.get(id=6)))
    streets_lines.append(StreetsLine(start_node=StreetsNode.objects.get(id=6), end_node=StreetsNode.objects.get(id=7)))
    streets_lines.append(StreetsLine(start_node=StreetsNode.objects.get(id=7), end_node=StreetsNode.objects.get(id=8)))
    streets_lines.append(StreetsLine(start_node=StreetsNode.objects.get(id=8), end_node=StreetsNode.objects.get(id=9)))
    streets_lines.append(StreetsLine(start_node=StreetsNode.objects.get(id=9), end_node=StreetsNode.objects.get(id=10)))
    streets_lines.append(StreetsLine(start_node=StreetsNode.objects.get(id=10), end_node=StreetsNode.objects.get(id=11)))

    for line in streets_lines:
        line.save()

    # create markers
    markers = list()
    markers.append(Marker(user=User.objects.get(username='brunokam'), longitude=19.90640, latitude=50.03030, title='Punkt 1', description='lala'))

    for marker in markers:
        marker.save()