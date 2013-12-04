# Pliki : 

# 2) dane pod model0a/model0c : model0c.data
# 3) graf w gml warsaw_graph.gml


### Baseline model - to test predictive power of the data samples ###
### Assumes data in the format given by data_helper ###

# TODO: move part of this functionality to the data_helper.r
# TODO: util.prepareDF, util.prepareDFTest

library(RSNNS)
library(nnet)
library(igraph)

### Generate samples ###
source("data_helper.r")
source("graph_helper.r")
#library(rjson)
#train = util.load_train_samples(increment = 10)
#print(length(train$samples))
#save(train, file="traffic_test_samples_inc10.RDa")





#load(file="data/traffic_test_samples_inc10.RDa")
#print(paste("loaded", length(train$samples), "samples"))

#train$samples = train$samples[1:2000]
#save(train, file="traffic_test_samples_inc10_subset.RDa")



#Note: mozna i tak w sumie , zakladajac ze to sie nazywa train






### Data regression 0b model ###


### Has to be specified beforehand - globally used###
model0c.getDefaultPredictOptions <- function(){
    #Note: specifies data format, each node can do the way it wants
    return(list(look_behind = 30, look_behind_prediction_self=30, look_behind_prediction = 10, ahead = 10, link_count=20))
}

model0c.prepareData <- function(train = train, predict_options, isTrain = T){
  	train$link_count = dim(train$samples[[1]]$x)[2] 
	train$minutes_sample = dim(train$samples[[1]]$x)[1]
	train$sample_count = length(train$samples)


	  ### Prepare data for regression ###
        # data_object - object specified by data_helper. r, shared data format
            # amongs models ###
        # predict - options of prediction ###
        # subset_x - variables taken as training
    
    ### Transform the data into desired format ###
    ###predict_look_behind = 10 # 10 minutes behind
    ###predict_ahead = 10 # window span to predict ahead
    ###predict_link_count = 1 # length of (predict_link_id)

    predict_options$x_size = predict_options$link_count*predict_options$look_behind
    
    if(isTrain == T) predict_options$y_size = predict_options$ahead*predict_options$link_count
    else predict_options$y_size = 0


    ### Prepare general data matrix (often will be too big - take it into account
    ### in the future)

    data_col = matrix(0, nrow = train$sample_count, ncol = predict_options$x_size + predict_options$y_size)

    indexes = (as.integer((0:(predict_options$x_size-1) )/predict_options$look_behind) + 1)
    indexes_y = (as.integer((0:(predict_options$y_size-1) )/predict_options$ahead) + 1)
    if(isTrain == T){
        indexes_time = rep(1:predict_options$look_behind, (train$link_count))
        indexes_time_y = rep(1:predict_options$ahead, (predict_options$ahead))
        colnames(data_col) = c(paste("L", indexes, "T", indexes_time, sep=""), paste("Y", indexes_y, "T", indexes_time_y, sep=""))
    }else{
        
        indexes_time = rep(1:predict_options$look_behind, (train$link_count))
        colnames(data_col) = paste("L", indexes, "T", indexes_time, sep="") 
    }

    ### Format L1T1... L1TK  ...   LNT1 .. LNTK Y1T1 .. Y1TH .. YWT1 .. YWTH ####
    ### K - minutes behind, N - number of links, Y - link ahead predict, H -
    ### minutes to predict ###

    ### Process individual samples ###
    for(i in 1:train$sample_count){ 
        train$samples[[i]]$x = train$samples[[i]]$x[(train$minutes_sample - predict_options$look_behind+1):train$minutes_sample,] # get specified time sample
        train$samples[[i]]$x = c(as.matrix(train$samples[[i]]$x)) 
        data_col[i,1:predict_options$x_size] = train$samples[[i]]$x

        if(isTrain == T){
            train$samples[[i]]$y = as.matrix(train$samples[[i]]$y)  
            train$samples[[i]]$y = as.matrix(train$samples[[i]]$y[1:predict_options$ahead, ]) # choose look ahead interval
            data_col[i, (predict_options$x_size+1):ncol(data_col)] = c(train$samples[[i]]$y)
        }
    }


    return(as.data.frame(data_col))
}


model0c.saveDataSample <- function(){
    predict_options = model0c.getDefaultPredictOptions()
    predict_options$look_behind = 30
    model0c.data = model0c.prepareData(train, predict_options)
    print(summary(model0c.data))
    save(model0c.data, file = "model0c.data.RDa")
    print("Done")
}


#model0c.saveDataSample()


### Train list of models (each for each time_step) for one link ###
### We understand : one model is a list of models for all the time_steps ###
### So called : ahead models ###
### Copying a big data matrix over to the function : waste of resources!
model0c.getmodel0c <- function(train_data, subset_x, subset_y, predict_options, weights_regression= -1){

       
 

    XVal<-as.matrix(train_data[,subset_x])
   
    XValCopy = matrix(0, ncol = 1, nrow = dim(XVal)[1])
    for(i in 1:as.integer(dim(XVal)[2]/5)){
        for(j in 1:dim(XVal)[1])
            XVal[j,i] = sum(XVal[j,  ((i-1)*5+1): (i*5)])
        XValCopy = cbind(XValCopy, XVal[,i])
    }

    XVal = XValCopy[,2:ncol(XValCopy)]

    #XVal = XVal[,1:10]
 
    #for(i in 1:dim(XVal)[1]) XVal[i,1] = sum(XVal[i,1:10])
    #for(i in 1:dim(XVal)[1]) XVal[i,2] = sum(XVal[i,11:20])
    #for(i in 1:dim(XVal)[1]) XVal[i,3] = sum(XVal[i,21:30])

    #for(i in 1:dim(XVal)[1]) XVal[i,4] = sum(XVal[i,31:40])
    #for(i in 1:dim(XVal)[1]) XVal[i,5] = sum(XVal[i,41:50])
    #for(i in 1:dim(XVal)[1]) XVal[i,6] = sum(XVal[i,51:60])

    #XVal = XVal[,1:6]
    XTarget<-as.matrix(train_data[,subset_y])

    print(dim(XTarget))
    print(dim(XVal))



    #TODO: pozbyc sie tego (inny format danych)
    for(i in 1:dim(XTarget)[1]) XTarget[i,1] = sum(XTarget[i,1:5])
    
    for(i in 1:dim(XTarget)[1]) XTarget[i,2] = sum(XTarget[i,6:10])

    predict_y = 2

    normXVal = max(max(XVal))
    normXTarget = max(max(XTarget[,1:predict_y]))   





    XVal = XVal/normXVal
    XTarget = XTarget/normXTarget

    

    model <- mlp(XVal, XTarget[,1:predict_y], size = 3,learnFuncParams=c(1), learnFunc = "BackpropWeightDecay", maxit = 400)

#    model<-nnet(x=as.matrix(XVal),y=as.matrix(XTarget[,1:predict_y]),maxit = 400, MaxNWts = 20000, size=10, rang=0.6, decay =0.1)
    
    yy = predict(model, XVal[1:1000,]) 

    yy = yy*normXTarget
    XTarget = XTarget*normXTarget


    print(yy[1:10,1:predict_y])   
    print(XTarget[1:10,1:predict_y]) 

    print(sqrt(mean((XTarget[1:1000,1:predict_y] - yy)^2)))

    return( list(ahead_models = model, x_params = subset_x, y_params = subset_y, normX = normXVal, normY = normXTarget))
    #ireturn( list(ahead_models = model, x_params = which(colnames(train_data) %in% subset_x), y_params = which(colnames(train_data) %in% subset_y), normX = normXVal, normY = normXTarget))
}


### Deploy model0 on a given sample ###
model0c.applymodel0c<-function(i,test_data, predict_options){ 

    return(sum(model0c.model[[i]]$normY*(predict(model0c.model[[i]]$ahead_models, as.vector(test_data)/model0c.model[[i]]$normX)))) #value to return - number of cars sumed in 10 minute period .. ups
}

### Perform whole (required) prediction ###
model0c.makePrediction <- function(x, predict_options){


   y = matrix(0, ncol = predict_options$link_count, nrow = 1)

   for(i in 1:predict_options$link_count){
        y[,i] = model0c.applymodel0c(i,as.vector(x[model0c.model[[i]]$x_params]),predict_options) 
   } 
  
   return(y)
}






link_models = list()
predict_options_tmp = model0c.getDefaultPredictOptions() # globally defined



### data_file_name - nazwa pliku .RDa w ktorym jest macierz X,Y i nazwy
### fajna separacja!

model0c.train<-function(data_file_name, predict_options, save_file_name){
	model0c.model = list()

	load(data_file_name) #to jest juz w formacie macierzy z wierszem X,Y

	g = read.graph(file = "data/warsaw_graph.gml", format = "gml")
	subgraph <- get_subgraph(g)
	corr <- get_distance_corr(subgraph)

	#Code for learning model0c
	weights_regression = c()
	for(i in 1:predict_options$link_count){
    		subset_y = c(paste("Y",i,"T",1:10,sep=""))
    		dist_corr = order(corr[,i])
    		subset_x=c()
     
   		for(j in 1:3){
     		 start_index = 30 - predict_options$look_behind_prediction + 1
    	    if(j==i) start_index = 30 - predict_options$look_behind_prediction_self + 1
    	   	 subset_x = c(subset_x,c(paste(paste("L",dist_corr[j],sep=""),"T",start_index:30, sep="")))
    	 	}
        print("train model")
 	   	model0c.model[[i]] = model0c.getmodel0c(model0b.data[,union(subset_x,subset_y)], subset_x, subset_y, predict_options, c())
	    
	}

	save(model0c.model, file = save_file_name)
}





predict_options = predict_options_tmp
#print(predict_options$link_count)
print("start training")
model0c.train("data/model0b.data.RDa", predict_options_tmp, "data/model0c.trained_model.RDa")
load(file = "data/model0c.trained_model.RDa")
print(summary(model0c.model))




test = util.load_test() # isTrain included
data_transform = model0c.prepareData(test, predict_options, isTrain = F) ## to tez bedzie do wymienienia 



y=(model0c.makePrediction(data_transform[1,], predict_options))

#print("done")
y_collected = y
for(i in 2:20){     ##dim(data_transform)[1]){
    if(i%%10==0) print(i)
    y_collected = rbind(y_collected, model0c.makePrediction( data_transform[i,], predict_options))
}
print(summary(y_collected))

#save(y_collected, file="tmp.RDa")
#print(dim(y_collected))

print(util.evaluate.matrix(y_collected))

#test_data = util.load_test()
#print(summary(test_data))









