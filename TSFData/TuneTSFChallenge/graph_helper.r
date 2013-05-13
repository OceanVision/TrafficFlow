#create_igraph <- fun
rm(list = ls())


create_igraph<- function(file_nodes = "data/street_graph_nodes.csv", file_edges = "data/street_graph_edges.csv"){

    nodes = read.csv(file_nodes, header = T, sep= " ")
    edges = read.csv(file_edges, header = T, sep= " ")



    require(igraph)
    library(igraph)

    g = graph.empty() 
    g = add.vertices(g, dim(nodes)[1])


    nodes_id = as.vector(nodes[,"Node_id"])

    ### import context ###
    V(g)$latitude = as.vector(nodes[,"Latitude"])
    V(g)$longitude = as.vector(nodes[, "Longitude"])
    V(g)$openid = nodes_id # openstreet map ID


    ### prepare edge list ###
    heads = rep(0, dim(edges)[1]) 
    tails = heads
    distance = rep(0, dim(edges)[1])
    avg_cars = rep(0, dim(edges)[1])
    lanes = rep(0, dim(edges)[1])
    edges_conc = c()
    for(i in 1:dim(edges)[1]){
        heads[i] = which(nodes_id == as.numeric(edges[i,"Node1_id"]))[1]
        tails[i] = which(nodes_id == as.numeric(edges[i,"Node2_id"]))[1]
        edges_conc = cbind(edges_conc, heads[i], tails[i])
        lanes[i] = as.numeric(edges[i, "Nr_of_lanes"])
        distance[i] = as.numeric(edges[i, "Distance.km."])
    }

    g = add.edges(g,edges_conc)
    E(g)$distance = distance
    E(g)$lanes = lanes
    E(g)$avgcard = avg_cars
    
    return(g)
} 
#g = create_igraph()
#write.graph(g, file = "warsaw_graph.gml", format = "gml")


m = read.csv(file = "data/streets.txt", head = T, sep= "")




g = read.graph(file = "warsaw_graph.gml", format = "gml")
### Create subgraph of nodes measured with ILD ###
subgraph = graph.empty()  
for(i in 1:dim(m)[1]){ ## all edges

    
    if(!(as.numeric(m[i,"HeadOpenID"]) %in% V(subgraph)$open_id)){
        subgraph = add.vertices(subgraph, 1)
        V(subgraph)[vcount(subgraph)]$openid = as.numeric(m[i,"HeadOpenID"])
        graphid = which(V(g)$openid == as.numeric(m[i,"HeadOpenID"]))

        V(subgraph)[vcount(subgraph)]$graphid = graphid     
        V(subgraph)[vcount(subgraph)]$longitude = V(g)[graphid]$longitude
        V(subgraph)[vcount(subgraph)]$latitude =  V(g)[graphid]$latitude


        
        print(V(subgraph)$graphid)
        print(which(V(g)$openid == as.numeric(m[i,"HeadOpenID"])))
    }
}


### Plot subgraph (visualisation) ###
L = cbind(V(subgraph)$longitude, V(subgraph)$latitude)
plot.igraph(subgraph, vertex.size =2, vertex.label=NA, layout=L)


