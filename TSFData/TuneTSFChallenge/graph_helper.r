#create_igraph <- fun
rm(list = ls())

file_nodes = "data/street_graph_nodes.csv" # format jak z OpenStreetMap
file_edges = "data/street_graph_edges.csv" # format jak z OpenStreetMap


nodes = read.csv(file_nodes, header = T, sep= " ")
edges = read.csv(file_edges, header = T, sep= " ")



require(igraph)
library(igraph)

g = graph.empty() 
g = add.vertices(g, dim(nodes)[1])

V(g)$Latitude = nodes[,"Latitude"]
V(g)$Longitude = nodes[, "Longitude"]

print(summary(nodes))

