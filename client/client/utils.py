from django.contrib.auth import authenticate, login, logout
from django.contrib.auth.models import User
from models import StreetsNode, StreetsLine


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
    if request.user.is_authenticated:
        logout(request)
        return True
    else:
        return False


def sign_up(request):
    user = User.objects.create_user(username=request.POST['username'], password=request.POST['password'])
    user.save()


# ========== E X T R A S ==========
def create_exemplary_data():
    StreetsNode.objects.all().delete()

    # create street nodes
    street_nodes = list()
    street_nodes.append(StreetsNode(longitude=19.90607, latitude=50.02688, title='Kawiarnia 1', description=''))
    street_nodes.append(StreetsNode(longitude=19.91293, latitude=50.02975, title='Mechanik 2', description=''))
    street_nodes.append(StreetsNode(longitude=19.97607, latitude=50.02438, title='Firma 3', description=''))
    street_nodes.append(StreetsNode(longitude=19.91211, latitude=50.01912, title='Biblioteka 4', description=''))

    for node in street_nodes:
        node.save()

    # create street edges
    street_edges = list()
    start_node = StreetsNode.objects.get(id=1)
    end_node = StreetsNode.objects.get(id=2)
    street_edges.append(StreetsLine(start_node=start_node, end_node=end_node))

    start_node = StreetsNode.objects.get(id=3)
    end_node = StreetsNode.objects.get(id=1)
    street_edges.append(StreetsLine(start_node=start_node, end_node=end_node))

    start_node = StreetsNode.objects.get(id=4)
    end_node = StreetsNode.objects.get(id=2)
    street_edges.append(StreetsLine(start_node=start_node, end_node=end_node))

    for edge in street_edges:
        edge.save()