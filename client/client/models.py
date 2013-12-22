from django.db import models
from django.contrib.auth.models import User


class StreetsNode(models.Model):
    longitude = models.FloatField()
    latitude = models.FloatField()
    title = models.TextField(max_length=50)
    description = models.TextField(max_length=200)


class StreetsLine(models.Model):
    start_node = models.ForeignKey(StreetsNode, related_name='streetedge_start_nodes')
    end_node = models.ForeignKey(StreetsNode, related_name='streetedge_end_nodes')