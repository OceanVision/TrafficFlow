source("graph_helper.r")
library(igraph)


read.clump <- function(file, lines, clump, header = F){
    if(clump > 1){
        p = read.csv(file, skip = lines*(clump-1), 
            nrows = lines, header=header, sep=" ")

        names(p) = header
    } else {
        p = read.csv(file, skip = lines*(clump-1), nrows = lines,sep = " ")
    }
    return(p)
}

## depend_list[i] - list of links to be taken into model (+ weights? - not implemented)
## contract = get most imporant that will be used later on
## contract = depend_list[[i]][0] = i ALWAYS 
general.get_depend_list <- function(data_sample = -1){ 
    ### data_sample - small sample of data to calculate the depend list (which
    ### to train on)

    ## mozna do pliku zapisac potem ##

	g = read.graph(file = "data/warsaw_graph.gml", format = "gml")
    subgraph <- get_subgraph(g)
    corr <- get_distance_corr(subgraph)

    corr_list = list()


    for(i in 1:ncol(corr)){
        corr_list[[i]] = order(corr[,i])[1:5]
    }


    return(corr_list)
}


## TODO: simplifying assumption only take into consideration +5 min in the
## difference pairwise factors
#saves to data/model0b.[alg_name].observationalSample.RDa#
model0b.prepareSampleObservationalData<- function(files_names, sample_size, link_count, samples_all, predict_options){
    observationalSample = matrix(0, nrow = sample_size, ncol = predict_options$link_count)
    sample_list = sample(1:samples_all, sample_size)
    print(sample_list)
    for(i in 1:predict_options$link_count){
        print(paste("prepareSampleObservationData for ", i,sep=""))
       load(file = files_names[i]) 
        
       observationalSample[,i] = linktraindata$samples[sample_list, (ncol(linktraindata$samples) - predict_options$ahead +1 ): (ncol(linktraindata$samples)-predict_options$ahead + 1)]
       rm(linktraindata)
    }
    file_name = paste("data/model0b.",predict_options$name, ".observationalSample.txt",sep="")
    write.table(observationalSample, file= file_name, col.names = F, row.names =F)
}

######## NOTE: THOSE FUNCTIONS ARE ALWAYS CALLED IN A BLOCK AND THIS IS WHY I RM IN THE FISHINPAIRWISECALCULATION #######3
## do calculation and load into global memory neede sample ##
## NOTE: ASSUMES THAT DATA IS NORMALIZED 0---1 !! VERY IMPORTANT
general.initPairwiseCalculation<- function(data_sample_file = "data/traffic_test.txt", link_count, data_sample_file_lines = 60000, step_size = 0.1){
    ## Get representative sample of the data
    data.sample = as.matrix(read.csv(file = data_sample_file, header = F, sep = " "))
    print(dim(data.sample))
    # for(i in 1:as.integer(data_sample_file_lines/100)){ 
    #    data.sample = rbind(data.sample,as.matrix(read.clump(data_sample_file,1000,i))[sample(1:1000, 100)])
    #}

    ## Calculate correlations ##
    pairwise = matrix(0, link_count, link_count)
    for(i in 1:(link_count -1)){
        for(j in (i+1):link_count){
          pairwise[i,j] = cor(data.sample[,i],data.sample[,j])
        }
    }

    ### Save most correlated ###
    assign("global_pairwise_pick", order(c(abs(pairwise)), decreasing = T)[1:80], env = globalenv())

    print(global_pairwise_pick)


    dif_factors = list()
    ## Calculate difference pairwise factors ##
    ## Simply by iterating through all samples ##
    step_count = 1.0/step_size
    for(i in 1:length(global_pairwise_pick)){

        print(paste("Calculating difference pairwise factor #",i,sep=""))
        
        b = as.integer((global_pairwise_pick[i]-1) / link_count) +1 # column 

        a = global_pairwise_pick[i] -  (b-1)*link_count # row


        print(global_pairwise_pick[i])
        print(paste(a," conv ",b))

        if(b > a){
            tmp = a
            a = b
            b = tmp
        }

        dif_factor = as.vector(rep(1.0, step_count))

        step_a = as.integer(data.sample[,a]/step_size)
        step_b = as.integer(data.sample[,b]/step_size)
        dif_step = abs(step_a - step_b)
        for(h in 1:length(dif_step))
        {
            if(step_a[h] > 4 | step_b[h] > 4)
            dif_factor[dif_step[h]] = dif_factor[dif_step[h]] + 1
        }
        print(dif_factor)

        dif_factors[[global_pairwise_pick[i]]] = dif_factor

    }   
    dif_factors[[link_count*link_count]] = NA 

    assign("global_dif_factors", dif_factors, env = globalenv())
    assign("global_pairwise", pairwise, env =  globalenv())

}

## retrieve from global memory ##
## returns c(-1,.....) if not calculated in initPairwiseCalculation
general.getDifferencePairwiseFactor<-function(i,j, link_count, step_size){
    index = (j-1)*link_count + i
    states_count = as.integer(1.0/step_size) 
    if(is.numeric(global_dif_factors[[index]])== T){ #was it picked in general.initPairwiseCalculation() ?
         print(index)
         return(global_dif_factors[[index]])
    }
    else{
        return(rep(1.0, states_count))
    }
}

## remove sample ##
general.finishPairwiseCalculation <- function(){
    rm("global_pairwise")
    rm("global_pairwise_pick")
    rm("global_dif_factors")
}
######## NOTE: THOSE FUNCTIONS ARE ALWAYS CALLED IN A BLOCK AND THIS IS WHY I RM IN THE FISHINPAIRWISECALCULATION #######3


model0b.getCompleteDependList <- function(predict_options){
    load(file =  paste("data/model0b.",predict_options$name,".depend_list.RDa",sep=""))
    return(depend_list)
}

model0b.getDependList<-function(i, predict_options){
    load(file= paste("data/model0b.",predict_options$name,".depend_list.RDa",sep=""))
    return(depend_list[[i]])
}

model0b.prepareDependList<-function(file_name = "data/traffic_test.txt", predict_options){
    ###1. Read 100 lines and feed into get_corr_list
	print("Prepare depend list")
	print(file_name)
    data.sample = read.clump(file_name, 100, 1)
    depend_list = general.get_depend_list(data.sample)
    save(depend_list, file = paste("data/model0b.",predict_options$name,".depend_list.RDa",sep=""))
    return(depend_list)  
}


#depend_list = model0b.prepareDependList()
#source("general.r"); general.split_data(depend_list=depend_list)


general.getNormalizingFactors<-function(file_name = "data/traffic_test.txt", predict_options){

    normalizing_factors = rep(1, link_count)
    print("Calculating normalizing factors")
    if(normalize == T)
    for(i in 1:as.integer(nrows/clump_size)){
        data.sample = read.clump(file_name, clump_size, i)
        for(j in 1:link_count) normalizing_factors[j] = min(normalizing_factors[j], 1.0/ max(data.sample[, j]))        
    }
    return(normalizing_factors)

}

## TODO: write code to determine nrows from the file_name
# Splits data into separate .csv for each link
general.split_data<- function(file_name="data/traffic_training.txt", nrows = 60000, predict_options , normalize = F){
    depend_list = model0b.prepareDependList(predict_options = predict_options)

    data.sample = read.clump(file_name, 100, 1)
    ###2. Setup some helpful variables      
    link_count = ncol(data.sample)
    clump_size = 1000


    normalizing_factors = rep(1, link_count) # responsibility moved to prepareData

    #normalizing_factors = general.getNormalizingFactors(file_name = file_name, predict_options = predict_options)

    ###3. Read iteratively for each link and pick appropriate subsample
    for(i in 1:link_count){
        print(paste("Splitting data for ",i,sep=""))
        data.linktraindata = matrix(0, nrow = nrows, ncol = length(depend_list[[i]]))
        for(k in 1:as.integer(nrows/clump_size)){ 
            data.part = as.matrix(read.clump(file_name,clump_size,k))
            data.linktraindata[(clump_size*(k-1)+1):(clump_size*k),] = data.part[,as.vector(depend_list[[i]])]
        }
        linktraindata = as.matrix(data.linktraindata)
        for(k in 1:dim(linktraindata)[2]) linktraindata[,k] = linktraindata[,k]*normalizing_factors[depend_list[[k]]]
        write.table(linktraindata, file=paste("data/traffic_training_link",i,".txt",sep=""),sep=" ",row.names = F, col.names = F)
    }

}
