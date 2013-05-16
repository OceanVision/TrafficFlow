# Pliki : 
# 1) dane surowe : traffic_test_samples_inc10.RDa
# 2) dane pod model0a/model0b : model0b.data
# 3) graf w gml warsaw_graph.gml

rm(list = ls())
### Baseline model - to test predictive power of the data samples ###
### Assumes data in the format given by data_helper ###

# TODO: move part of this functionality to the data_helper.r
# TODO: util.prepareDF, util.prepareDFTest

LINK_COUNT = 20

library(igraph)

### Generate samples ###
source("data_helper.r")
source("graph_helper.r")
#library(rjson)
#train = util.load_train_samples(increment = 10)
#print(length(train$samples))
#save(train, file="traffic_test_samples_inc10.RDa")





load(file="traffic_test_samples_inc10.RDa")
print(paste("loaded", length(train$samples), "samples"))

#train$samples = train$samples[1:2000]
#save(train, file="traffic_test_samples_inc10_subset.RDa")


### Niech to bedzie juz wczesniej ###
train$link_count = dim(train$samples[[1]]$x)[2] 
train$minutes_sample = dim(train$samples[[1]]$x)[1]
train$sample_count = length(train$samples)
train$isTrain = T
### 





### Data regression 0a model ###
model0b.getDefaultPredictOptions <- function(){
    return(list(look_behind = 10, look_behind_self=30, ahead = 10,link_countlink_count= 1))
}

model0b.prepareData <- function(train = train, predict_options = list(look_behind = 10,ahead = 10,link_countlink_count= 1)){
    ### Prepare data for regression ###
        # data_object - object specified by data_helper. r, shared data format
            # amongs models ###
        # predict - options of prediction ###
        # subset_x - variables taken as training
    
    ### Transform the data into desired format ###
    ###predict_look_behind = 10 # 10 minutes behind
    ###predict_ahead = 10 # window span to predict ahead
    ###predict_link_count = 1 # length of (predict_link_id)

    predict_options$x_size = train$link_count*predict_options$look_behind
    
    if(train$isTrain == T) predict_options$y_size = TRAINDATA_PREDICTSIZE*train$link_count
    else predict_options$y_size = 0


    ### Prepare general data matrix (often will be too big - take it into account
    ### in the future)

    data_col = matrix(0, nrow = train$sample_count, ncol = predict_options$x_size + predict_options$y_size)

    indexes = (as.integer((0:(predict_options$x_size-1) )/predict_options$look_behind) + 1)
    indexes_y = (as.integer((0:(predict_options$y_size-1) )/predict_options$ahead) + 1)
    if(train$isTrain == T){
        indexes_time = rep(1:predict_options$look_behind, (train$link_count))
        indexes_time_y = rep(1:predict_options$ahead, (TRAINDATA_PREDICTSIZE))
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

        if(train$isTrain == T){
            train$samples[[i]]$y = as.matrix(train$samples[[i]]$y)  
            train$samples[[i]]$y = as.matrix(train$samples[[i]]$y[1:predict_options$ahead, ]) # choose look ahead interval
            data_col[i, (predict_options$x_size+1):ncol(data_col)] = c(train$samples[[i]]$y)
        }
    }


    return(as.data.frame(data_col))
}


model0b.saveDataSample <- function(){
    predict_options = model0b.getDefaultPredictOptions()
    predict_options$look_behind = 30
    model0b.data = model0b.prepareData(train, predict_options)
    print(summary(model0b.data))
    save(model0b.data, file = "model0b.data.RDa")
    print("Done")
}


#model0b.saveDataSample()


### Train list of models (each for each time_step) for one link ###
### We understand : one model is a list of models for all the time_steps ###
### So called : ahead models ###
model0b.getModel0b <- function(train_data, subset_x, subset_y, predict_options, weights_regression= -1){
    model.data = train_data[,union(subset_x, subset_y)]

    print(cat("### Doing model for ", subset_x, "###"))

    model.x_params = ncol(model.data) # poprawic jako liste
    model.test_size = 1000 # poprawic

    print("### Training regression model ###")
   
 
    models = list()
    for(y in 1:length(subset_y)){
        print(paste("Training ",y))
        m = lm(model.data[,subset_y[y]] ~ ., data = model.data[,subset_x])
        models[[y]] = list(coef = m$coef) #TODO: add information about error
    }

    rm(train_data)

    return( list(ahead_models = models, x_params = subset_x, y_params = subset_y))
}


### Deploy model0 on a given sample ###
model0b.applyModel0b<-function(model0b, test_data){
    linearPredict <- function(x, weights){
        return( sum(x*weights[2:length(weights)]) + weights[1])
    }
    test_data = as.vector(as.matrix(test_data))
    predicted = rep(0, TRAINDATA_PREDICTSIZE)
    for(i in 1:TRAINDATA_PREDICTSIZE){#train.sample_count){
        predicted[i] = linearPredict(test_data, model0b$ahead_models[[i]]$coef) #predict
    }
    return(sum(predicted)) #value to return - number of cars sumed in 10 minute period .. ups
}

### Perform whole (required) prediction ###
model0b.makePrediction <- function(link_models, x){
   y = matrix(0, ncol = LINK_COUNT, nrow = 1)
   for(i in 1:LINK_COUNT){
        y[,i] = model0b.applyModel0b(link_models[[i]], as.vector(x[link_models[[i]]$x_params])) 
   } 
   return(y)
}





link_models = list()

predict_options = model0b.getDefaultPredictOptions()
predict_options$look_behind = 10

load("model0b.data.RDa")



g = read.graph(file = "warsaw_graph.gml", format = "gml")
subgraph <- get_subgraph(g)
corr <- get_distance_corr(subgraph)


print(corr[,4])
print(order(corr[,4]))
print(summary(subgraph))


#TODO: include atleast itself!! lol trorlrorlwrjw
#Code for learning model0a
print(LINK_COUNT)
weights_regression = c()
for(i in 1:LINK_COUNT){
    subset_y = c(paste("Y",i,"T",1:10,sep=""))
    dist_corr = order(corr[,i])
    subset_x=c()
     
    for(j in 1:10){
        start_index = 30 - predict_options$look_behind + 1
        if(j==0) start_index = 30 - predict_options$look_behind_self + 1
        subset_x = c(subset_x,c(paste(paste("L",dist_corr[j],sep=""),"T",start_index:30, sep="")))
     }
    link_models[[i]] = model0b.getModel0b(model0b.data[,union(subset_x,subset_y)], subset_x, subset_y, predict_options, c())
    print(summary(link_models[[i]]$ahead_models[[1]]))
}

#save(link_models, file="models0a.link_models.fixed_train.RDa")
#load(file = "models0a.link_models.fixed_train.RDa")
#rm(model0a.data.3010)

test = util.load_test() # isTrain included
predict_options$look_behind = 30
data_transform = model0b.prepareData(test, predict_options) 
predict_options$look_behind = 10
y=(model0b.makePrediction(link_models, data_transform[1,]))

print("done")
y_collected = y
for(i in 2:dim(data_transform)[1]){
    if(i%%100==0) print(i)
    y_collected = rbind(y_collected, model0b.makePrediction(link_models, data_transform[i,]))
}
print(summary(y_collected))

save(y_collected, file="tmp.RDa")
#print(dim(y_collected))

print(util.evaluate.matrix(y_collected))

#test_data = util.load_test()
#print(summary(test_data))









