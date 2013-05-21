source("general.r")
### Constant section ###
TRAINDATA_CYCLE = 600 # minutes in one cycle
TRAINDATA_COUNT = 100 # no of simulation cycles
TRAINDATA_SLOTSIZE = 30 # no of minute to take into account. NOTICE TRAINDATA CYCLE HAS TO BE 0 MOD TRAINDATA_SLOTSIZE
TRAINDATA_PREDICTSIZE = 10#  no of minutes to predict
TRAINDATA_INCREMENT = 30 # overlapping controlling
TRAINDATA_LINK_COUNT = 20 #number of links to predict for
### End of constant section ###


util.load_test <- function(file_name = "data/traffic_test.txt"){
    ### Read raw data ### 
    test.raw = as.matrix(read.csv(file=file_name,head=F,sep=" ",colClasses = rep("numeric",TRAINDATA_LINK_COUNT)))
    
    ### Prepare sample list basing on constants ###
    test.samples = list()
    at = 1; sample_count = 0
    while(1){
        sample_count = sample_count + 1
        test.samples[[sample_count]] = list(x = test.raw[at:(at+TRAINDATA_SLOTSIZE-1),])
        at = at + TRAINDATA_SLOTSIZE
        if(at > dim(test.raw)[1]) break
    }
    return(list(raw = test.raw, samples = test.samples, link_count = dim(test.samples[[1]]$x)[2], minutes_sample = dim(test.samples[[1]]$x)[1], sample_count = length(test.samples), isTrain = F))
}

### wrapper to get only samples ###
util.load_train_samples <- function(file_name = "data/traffic_training.txt", increment = -1){
    train = util.load_train(file_name, increment)   
 
    return(list(samples = train$samples))
}

util.load_train <- function(file_name = "data/traffic_training.txt", increment = -1, predict_for = -1){
    if(increment == -1) increment = TRAINDATA_INCREMENT
    ### Read raw data ### 
    train.raw = read.csv(file=file_name,head=F,sep=" ")


    if(predict_for == -1) predict_for = dim(train.raw)[2]

    plot(1:100,as.vector(train.raw[1:100,1]),"l")

    train.cycles = list()
    for(i in 1:TRAINDATA_COUNT){
        start_index = (i-1)*(TRAINDATA_CYCLE) + 1
        end_index = i*TRAINDATA_CYCLE
        train.cycles[[i]] = train.raw[start_index:end_index, ]
    }

    ### Prepare sample list basing on constants ###
    train.samples = list()
    at = 1; sample_count = 0
    at_cycle = 1;
    while(1){

        sample_count = sample_count + 1
        train.samples[[sample_count]] = list(x = as.matrix(train.raw[at:(at+TRAINDATA_SLOTSIZE-1),]), y = as.matrix(train.raw[(at+TRAINDATA_SLOTSIZE):(at+TRAINDATA_SLOTSIZE+TRAINDATA_PREDICTSIZE-1),1:predict_for]))
        at_cycle = at %% TRAINDATA_CYCLE

        
        if(at_cycle + TRAINDATA_SLOTSIZE + TRAINDATA_PREDICTSIZE > TRAINDATA_CYCLE){
            at_cycle = 0
            at = TRAINDATA_CYCLE*as.integer(at/TRAINDATA_CYCLE) + TRAINDATA_CYCLE + 1
        } else{
            at_cycle = at %% TRAINDATA_CYCLE
            at = at + increment 
        }

        if(at > dim(train.raw)[1]) break
    }


    return(list(samples = train.samples, isTrain = T))
}

## evaluate solution stored in matrix ###
util.evaluate.matrix <- function(test_matrix, target_file = "solutions/test_priv.txt", predict_options){
    require("hydroGOF")
    library("hydroGOF") ## rmse implementation ##
    sol = read.csv(file = target_file,head=F,sep=" ")
	print(dim(sol))    
	
    return(rmse(as.vector(as.matrix(sol)),as.vector(as.matrix(test_matrix))))
}

## evaluate solution from file ##
util.evaluate.file <- function(test_file = "solutions/traffic_example.txt", target_file = "solutions/test_priv.txt"){
    require("hydroGOF")
    library("hydroGOF") ## rmse implementation ##
    target = read.csv(file = test_file,head=F,sep=" ")
    sol = read.csv(file = target_file,head=F,sep=" ")
    print(dim(target))
    return(rmse(as.vector(as.matrix(target)),as.vector(as.matrix(sol))))
}


