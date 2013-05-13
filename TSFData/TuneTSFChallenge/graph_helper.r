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

get_subgraph <- function(g, file = "data/streets.txt"){
    m = read.csv(file = "data/streets.txt", head = T, sep= "")

    ### create subgraph of nodes measured with ild ###
    subgraph = graph.empty()  
    #add vertices
    for(i in 1:dim(m)[1]){ ## all edges

        
        if(!(as.numeric(m[i,"HeadOpenID"]) %in% V(subgraph)$openid)){
            subgraph = add.vertices(subgraph, 1)
            V(subgraph)[vcount(subgraph)]$openid = as.numeric(m[i,"HeadOpenID"])
            graphid = which(V(g)$openid == as.numeric(m[i,"HeadOpenID"]))

            V(subgraph)[vcount(subgraph)]$graphid = graphid     
            V(subgraph)[vcount(subgraph)]$longitude = V(g)[graphid]$longitude
            V(subgraph)[vcount(subgraph)]$latitude =  V(g)[graphid]$latitude
        }
        
        if(!(as.numeric(m[i,"TailOpenID"]) %in% V(subgraph)$openid)){
            subgraph = add.vertices(subgraph, 1)
            V(subgraph)[vcount(subgraph)]$openid = as.numeric(m[i, "TailOpenID"])
            graphid = which(V(g)$openid == as.numeric(m[i,"TailOpenID"]))

            V(subgraph)[vcount(subgraph)]$graphid = graphid     
            V(subgraph)[vcount(subgraph)]$longitude = V(g)[graphid]$longitude
            V(subgraph)[vcount(subgraph)]$latitude =  V(g)[graphid]$latitude
        }
    
    }
    #add edges
    edges = c()
    for(i in 1:dim(m)[1]){ ## all edges
        head_id = which(V(subgraph)$openid == as.numeric(m[i,"HeadOpenID"]))
        tail_id = which(V(subgraph)$openid == as.numeric(m[i,"TailOpenID"]))
        edges = cbind(edges, head_id, tail_id)
    }

    subgraph = add.edges(subgraph, edges)

    return(subgraph)
}

plot_graph_with_gps_data <- function(g){
    ### Plot subgraph (visualisation) ###
    L = cbind(V(g)$longitude, V(g)$latitude)
    #png("tmp.png",width=640,height=640) # save to file
    plot.igraph(g, vertex.size =2, vertex.label=NA, layout=L)
    #dev.off()
}


# usecase1 of basic functionality #
usecase1 <- function(){
    g = create_igraph()
    write.graph(g, file = "warsaw_graph.gml", format = "gml")

    print(summary(g))

    subgraph <- get_subgraph(g)

    plot_graph_with_gps_data(subgraph)
}


usecase1()
