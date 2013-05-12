
### Constant section ###
TRAINDATA_CYCLE = 600 # minutes in one cycle
TRAINDATA_COUNT = 100 # no of simulation cycles
TRAINDATA_SLOTSIZE = 30 # no of minute to take into account. NOTICE TRAINDATA CYCLE HAS TO BE 0 MOD TRAINDATA_SLOTSIZE
TRAINDATA_PREDICTSIZE = 10#  no of minutes to predict
TRAINDATA_INCREMENT = 30 # overlapping controlling
TRAINDATA_LINK_COUNT = 20 # number of observed links
### End of constant section ###


util.load_test <- function(file_name = "traffic_test.txt"){
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
    return(list(raw = test.raw, samples = test.samples, link_count = dim(test$samples[[1]]$x)[2], minutes_sample = dim(test$samples[[1]]$x)[1], sample_count = length(test$samples), isTrain = F))
}

util.load_train <- function(file_name = "traffic_training.txt"){
    ### Read raw data ### 
    train.raw = read.csv(file=file_name,head=F,sep=" ")
    train.cycles = list()
    for(i in 1:TRAINDATA_COUNT){
        start_index = (i-1)*(TRAINDATA_CYCLE) + 1
        end_index = i*TRAINDATA_CYCLE
        train.cycles[[i]] = train.raw[start_index:end_index, ]
    }

    ### Prepare sample list basing on constants ###
    train.samples = list()
    at = 1; sample_count = 0
    while(1){
        sample_count = sample_count + 1
        train.samples[[sample_count]] = list(x = train.raw[at:(at+TRAINDATA_SLOTSIZE-1),], y = train.raw[(at+TRAINDATA_SLOTSIZE):(at+TRAINDATA_SLOTSIZE+TRAINDATA_PREDICTSIZE-1),])
        at = at + TRAINDATA_INCREMENT
        if(at > dim(train.raw)[1]) break
    }
    return(list(raw = train.raw, cycles = train.cycles, samples = train.samples))
}

## evaluate solution stored in matrix ###
util.evaluate.matrix <- function(test_matrix, target_file = "solutions/test_priv.txt"){
    require("hydroGOF")
    library("hydroGOF") ## rmse implementation ##
    sol = read.csv(file = target_file,head=F,sep=" ")
    return(rmse(as.vector(as.matrix(sol)),as.vector(as.matrix(test_matrix))))
}

## evaluate solution from file ##
util.evaluate.file <- function(test_file = "solutions/traffic_example.txt", target_file = "solutions/test_priv.txt"){
    require("hydroGOF")
    library("hydroGOF") ## rmse implementation ##
    target = read.csv(file = test_file,head=F,sep=" ")
    sol = read.csv(file = target_file,head=F,sep=" ")
    return(rmse(as.vector(as.matrix(target)),as.vector(as.matrix(sol))))
}
