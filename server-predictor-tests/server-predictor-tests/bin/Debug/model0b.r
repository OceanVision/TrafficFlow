#TODO: problems with downgrading matrix to vector:

# Pliki : 
# 1) dane surowe : traffic_test_samples_inc10.RDa
# 2) dane pod model0a/model0b : model0b.data
# 3) graf w gml warsaw_graph.gml


### Baseline model - to test predictive power of the data samples ###
### Assumes data in the format given by data_helper ###

# TODO: move part of this functionality to the data_helper.r
# TODO: util.prepareDF, util.prepareDFTest



library(igraph)

### Generate samples ###
source("data_helper.r")
source("general.r")
source("graph_helper.r")
#library(rjson)
#train = util.load_train_samples(increment = 10)
#print(length(train$samples))
#save(train, file="traffic_test_samples_inc10.RDa")



### Data regression 0b model ###


### Predict_options description ###

# ahead - ahead numbers to predict in the time_series
# ahead_data - ahead number included in the data (may be needed to be summed out)
# look_behind_train - look behind included in the data (NOT IMPLEMENTED)
# look_behind - may be needed to sum out (NOT IMPLEMENTED)

### End of predict_options description ###

### Has to be specified beforehand - **globally** used###
model0b.getDefaultPredictOptions <- function(){
    #Note: specifies data format, each node can do the way it wants
    return(list(
look_behind = 30,             #number of steps to include into prediction as it is in THE DATA  #quite global 
look_behind_prediction_self=30,  #number of steps to look behind when considering the link to predict   
look_behind_prediction = 10,  #-,- other links
name = "smrCars", #algorithm name for naming purposes
ahead = 2, #number of steps to predict
ahead_data = 10, #actual number of steps registered in the file 
link_count=20, #number of all links int he network to predict for (every link will separately predict for it subset of dependency list)
take_neigh_count =10)
)
	}


## Simple function for normalizing data ##
model0b.normalizeData<- function(dataobject, predict_options, isTrain = T){
    #for(i in 1:dim(dataobject$samples)[1]){

    if(isTrain == T)
    for(j in 1:(dim(dataobject$samples)[2] - predict_options$ahead)){
         dataobject$samples[,j] = dataobject$samples[,j]*dataobject$normalize_factors_x[ as.integer((j-1)/predict_options$look_behind) + 1]
    }
    else
    for(j in 1:(dim(dataobject$samples)[2])){
         dataobject$samples[,j] = dataobject$samples[,j]*dataobject$normalize_factors_x[ as.integer((j-1)/predict_options$look_behind) + 1]
    }
    if(isTrain == T)   dataobject$samples[, (ncol(dataobject$samples) - predict_options$ahead + 1):ncol(dataobject$samples)] = dataobject$normalize_factor_y *   dataobject$samples[, (ncol(dataobject$samples) - predict_options$ahead + 1):ncol(dataobject$samples)] 
    #}
    return(dataobject) 
}
## Simple function for normalizing data ##
model0b.deNormalizeData<- function(dataobject, predict_options){
    #for(i in 1:dim(dataobject$samples)[1]){
    for(j in 1:(dim(dataobject$samples)[2] - predict_options$ahead)){
         dataobject$samples[,j] = dataobject$samples[,j]*(1.0/dataobject$normalize_factors_x[ as.integer((j-1)/predict_options$look_behind) + 1])
    }
    dataobject$samples[, (ncol(dataobject$samples) - predict_options$ahead + 1):ncol(dataobject$samples)] = (1.0/dataobject$normalize_factor_y) *   dataobject$samples[, (ncol(dataobject$samples) - predict_options$ahead + 1):ncol(dataobject$samples)] 
    #}
    return(dataobject) 
}
## From raw TSF DATA ##
model0b.tsfprepareAllDataAndSave <- function(pref = "data/traffic_training_link", suf=".txt", link_count = 20, save_pref = "data/model0b.link", save_suf = ".data.RDa", predict_options){


    normalize_factors_x = rep(1, predict_options$link_count)
    normalize_factors_y = rep(1, link_count)
    #Iterate for each link

    depend_list = model0b.getCompleteDependList(predict_options = predict_options) #should be included into predict_options : part of the model specification

    print("*** prepare and save unnormalized data ***")
    a=1
    if(a==1)
    for(w in 1:link_count){
        print(paste("********** tsfPreapre #",w,sep=""))
        file_name = toString(paste(pref,w,suf,sep=""))
        train = util.load_train(file_name,increment = 10, predict_for = 1)
        linktraindata = model0b.prepareData(train, predict_options, isTrain = T)



        ## take into account depend lists ##
        print(paste("******* tsfPreapre #",w,"_getNormalization",sep=""))
        
        for(i in 1:length(depend_list[[w]])){
            normalize_factors_x[depend_list[[w]][i]] = min(normalize_factors_x[depend_list[[w]][i]],  1/max(linktraindata$samples[, ((i-1)*predict_options$look_behind+1):(i*predict_options$look_behind)]))
        }
    
        normalize_factors_y[w] = min(normalize_factors_y[w] , 1.0/max(linktraindata$samples[, (ncol(linktraindata$samples)-predict_options$ahead+1): ncol(linktraindata$samples)]))
    

        print(normalize_factors_x)
        print(normalize_factors_y)
        save(linktraindata, file = paste(save_pref, w, save_suf, sep=""))
        rm(linktraindata)
    }
    print("**normalize data**")
    for(w in 1:link_count){
        print(paste("********** tsfNormalize #",w,sep=""))
        file_name = toString(paste(pref,w,suf,sep=""))
        load(file = paste(save_pref, w, save_suf, sep=""))
       
        print(max(linktraindata$samples))
        #very convienient
        linktraindata$normalize_factors_x = normalize_factors_x[depend_list[[w]]] 
        linktraindata$normalize_factor_y = normalize_factors_y[w]  

        print(linktraindata$normalize_factors_x)

        linktraindata = model0b.normalizeData(linktraindata, predict_options)
       
        print("Sanity check")
        print(max(linktraindata$samples))

        print(linktraindata$samples[1,]) 
        save(linktraindata, file = paste(save_pref, w, save_suf, sep=""))
        rm(linktraindata)
    } 
    print("***save generals**")
    file_name = paste(save_pref,".general",save_suf,sep="")
    linktraindata.general = list()
    linktraindata.general$normalize_factors_x = normalize_factors_x
    linktraindata.general$normalize_factors_y = normalize_factors_y
    save(linktraindata.general, file = file_name)
}

model0b.prepareData <- function(train = train, predict_options, isTrain = T, predict_for = 1){
	### Read counts ###
	train$link_count = dim(train$samples[[1]]$x)[2] 
	train$minutes_sample = dim(train$samples[[1]]$x)[1] # = look_behind, repair later
	train$sample_count = length(train$samples)


    print(paste("Link count = ",train$link_count,sep=""))

	  ### Prepare data for regression ###
        # data_object - object specified by data_helper. r, shared data format
            # amongs models ###
        # predict - options of prediction ###
        # subset_x - variables taken as training
    
    ### Transform the data into desired format ###
    ###predict_look_behind = 10 # 10 minutes behind
    ###predict_ahead = 10 # window span to predict ahead
    ###predict_link_count = 1 # length of (predict_link_id)

    ### Auxiliary predict_options params#
    train$x_size = train$link_count*predict_options$look_behind
    if(isTrain == T) train$y_size = predict_options$ahead * 1 # Only itself int he model0b.r (all int he model0bindividual.r)
    else train$y_size = 0

    


    ### Prepare general data matrix (often will be too big - take it into account
    ### in the future)
    data_col = matrix(0, nrow = train$sample_count, ncol = (train$x_size + train$y_size))

    print(dim(data_col))

    ### Format L1T1... L1TK  ...   LNT1 .. LNTK Y1T1 .. Y1TH .. YWT1 .. YWTH ####
    ### K - minutes behind, N - number of links, Y - link ahead predict, H -
    ### minutes to predict ###

    ### Process individual samples ###
    for(i in 1:train$sample_count){ 
        if(i%%100 == 0) print(i)

        train$samples[[i]]$x = train$samples[[i]]$x[(train$minutes_sample - predict_options$look_behind+1):train$minutes_sample,] # get specified time sample
        train$samples[[i]]$x = c(as.matrix(train$samples[[i]]$x)) 
        data_col[i,1:train$x_size] = train$samples[[i]]$x
        

        if(isTrain == T){
            train$samples[[i]]$y = as.matrix(train$samples[[i]]$y)  
            train$samples[[i]]$y = as.matrix(train$samples[[i]]$y[1:predict_options$ahead_data, ]) # choose look ahead interval
		## SUMMING OUT ##
		if(predict_options$ahead != predict_options$ahead_data){
			#print("summing out")
			by_sum = as.integer(predict_options$ahead_data / predict_options$ahead)
			
			last_summed_out = 0
			index = 1
			while(1){
				#print(index)
				#print(last_summed_out)
				#train$samples[[i]]$y[index,] = as.matrix(colSums(train$samples[[i]]$y[((index-1)*by_sum+1):(index*by_sum),]))
				train$samples[[i]]$y[index] = sum(train$samples[[i]]$y[((index-1)*by_sum+1):(index*by_sum)])
				index = index +1
				last_summed_out = last_summed_out + by_sum 
				if(index > predict_options$ahead) break
				
			}
			#truncate summed out terms
			train$samples[[i]]$y = train$samples[[i]]$y[1:predict_options$ahead]
			#train$samples[[i]]$y = train$samples[[i]]$y[1:predict_options$ahead,1:predict_for]
			#print("done")
		}
		#print(train$samples[[i]]$y)
		data_col[i, (train$x_size+1):ncol(data_col)] = c(train$samples[[i]]$y)
        }
    }


    #Note y size cna be infered  from predict_options variable    
    return(list(samples = as.data.frame(data_col)))
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

#model0b.tsfprepareAllDataAndSave sets the prediction task and the rest of the code should be a general as it is possible

### Train list of models (each for each time_step) for one link ###
### We understand : one model is a list of models for all the time_steps ###
### So called : ahead models ###
model0b.getModel0b <- function(model.data,  predict_options, weights_regression= -1){

    print(cat("### Doing model for ", dim(model.data)[2]-predict_options$ahead, "observationals and ", dim(model.data)[1],"observations ###"))

    model.x_params = ncol(model.data) # poprawic jako liste
    model.test_size = 1000 # poprawic

    print("### Training regression model ###")
  
    subset_y = (ncol(model.data)-predict_options$ahead+1):ncol(model.data) # number of "step-aggregate" to predict
    models = list()
    for(y in 1:length(subset_y)){
        print(paste("Training ",y))
        m = lm(model.data[,subset_y[y]] ~ ., data = model.data[,1:(ncol(model.data)-predict_options$ahead)])
        models[[y]] = list(coef = m$coef) #TODO: add information about error
    }

    rm(train_data)
    return( list(ahead_models = models))
}


### Deploy model0 on a given sample ###
model0b.applyModel0b<-function(model0b, test_data, predict_options){
    linearPredict <- function(x, weights){
        return( sum(x*weights[2:length(weights)]) + weights[1])
    }
    test_data = as.vector(as.matrix(test_data))
    for(i in 1:length(test_data)) test_data[i] = test_data[i] * model0b$normalize_factors_x[as.integer((i-1)/predict_options$look_behind) + 1]
    predicted = rep(0, predict_options$ahead)
    for(i in 1:predict_options$ahead){#train.sample_count){
        predicted[i] = linearPredict(test_data[model0b$indices_x], model0b$ahead_models[[i]]$coef) #predict
    }
    predicted = predicted * (1.0/model0b$normalize_factor_y)
    ## !!!!!!! REMOVE THIS LINE !!!!!!!!!!!#
    return(c(predicted))
}

### Perform whole (required) prediction ###
model0b.makePrediction <- function(link_models, x, predict_options){
   y = matrix(0, ncol = predict_options$link_count, nrow = predict_options$ahead)
  
   depend_list = model0b.getCompleteDependList(predict_options) 

     for(i in 1:predict_options$link_count){
        indices = c()
        for(j in 1:length(depend_list[[i]])){
		            indices = c(indices, ((depend_list[[i]][j]-1)*predict_options$look_behind+1):(depend_list[[i]][j]*predict_options$look_behind))
        }
        y[,i] = model0b.applyModel0b(link_models[[i]], as.vector(x[indices]), predict_options) 
   } 

	##note: sligh change - Y1+1 Y1+2 Y2+1 Y2+2 format
   return(y)
}






link_models = list()
predict_options_tmp = model0b.getDefaultPredictOptions() # globally defined



## TODO: better design : separate completely model0b.r and write singularmodel.r - in
## the future



## Train linked model (move to singularmodel0b.r) ##
## data_file_names - vector of files with training data ##
model0b.train<-function(data_file_names, predict_options, save_file_name=""){
	model0b.model = list()

	for(i in 1:predict_options$link_count){
	    load(data_file_names[i]) #to jest juz w formacie macierzy z wierszem X,Y 
        #assumption : first link is my link, should store somewhere dependency list#
        indices = c()
        start_index = 1
 	   	#TODO: where to move this piece of responsibility?
        for(j in 1:length(model0b.getDependList(i,predict_options))){
             if(j==1) indices = c(indices, (start_index+predict_options$look_behind-1  - predict_options$look_behind_prediction_self+1):(start_index+predict_options$look_behind-1))
             else indices = c(indices, (start_index+predict_options$look_behind-1  - predict_options$look_behind_prediction+1):(start_index+predict_options$look_behind-1))
            start_index = start_index + predict_options$look_behind
        }
        indices = c(indices, (ncol(linktraindata$samples)-predict_options$ahead+1):ncol(linktraindata$samples))
        indicestmp = indices
        model0b.model[[i]] = c(model0b.getModel0b(linktraindata$samples[,indices], predict_options))
        model0b.model[[i]]$indices_x = indices[1:(length(indices)-predict_options$ahead)]
        model0b.model[[i]]$normalize_factors_x = linktraindata$normalize_factors_x
        model0b.model[[i]]$normalize_factor_y = linktraindata$normalize_factor_y
	}


	if(save_file_name!="") save(model0b.model, file = save_file_name)
    return(model0b.model)
}


model0b.calculateGloballyGraph<- function(){
	assign("global_g", read.graph(file = "data/warsaw_graph.gml", format = "gml"), envir = globalenv())
	assign("global_subgraph", get_subgraph(global_g), envir = globalenv())
	assign("global_corr" , get_distance_corr(global_subgraph), envir = globalenv())
}
### Assumes that model0b.calculateGloballyGraph() was called prior to this call ###
### To do: move to pairwise.r (better idea probably)
model0b.getMostImportantNeighbours<-function(id, how_many =5 ){
	
	print(id)
	most_important = order(global_corr[,id])
	return(most_important[1:how_many]) # model0b returns constant number of important edges
}

model0b.test<-function(){
    predict_options = model0b.getDefaultPredictOptions()
    traindata_files = paste("data/model0b.link",1:20,".data.RDa", sep="")
    model0b.model = model0b.train(traindata_files, predict_options_tmp, "data/model0b.trained_model.RDa")

    test = util.load_test() # isTrain included
    data_transform = model0b.prepareData(test, predict_options, isTrain = F, predict_for = 1) ## to tez bedzie do wymienienia 
    y=(model0b.makePrediction(model0b.model, data_transform$samples[1,], predict_options))


    #print(data_transform$samples[1,])

    #print("done")
    y_collected = y
    for(i in 2:dim(data_transform$samples)[1]){
        if(i%%100==0) print(i)
        y_collected = rbind(y_collected, model0b.makePrediction(model0b.model, data_transform$samples[i,], predict_options))
    }
    
    print(y_collected)

    load("data/model0b.link.general.data.RDa")
    

    #print(summary(y_collected))
    print(data_transform$samples[1,])
    #save(y_collected, file="tmp.RDa")
    #print(dim(y_collected))

 
    print(util.evaluate.matrix(y_collected))


    return(y_collected)
    #test_data = util.load_test()


    #print(summary(test_data))
}








