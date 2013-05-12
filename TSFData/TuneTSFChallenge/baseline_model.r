### Baseline model - to test predictive power of the data samples ###
### Assumes data in the format given by data_helper ###

# TODO: move part of this functionality to the data_helper.r
# TODO: util.prepareDF, util.prepareDFTest

load("traffic_test.RDa")

### Transform the data into desired format ###
predict_link_id = 4 # for which link should we predict
predict_look_behind = 10 # 10 minutes behind
predict_ahead = 10 # window span to predict ahead
predict_link_count = 1 # length of (predict_link_id)

### Niech to bedzie juz wczesniej ###
train.samples = train[["samples"]]
train.link_count = dim(train.samples[[1]]$x)[2] 
train.minutes_sample = dim(train.samples[[1]]$x)[1]
train.sample_count = length(train.samples)
### 

predict_x_size = train.link_count*predict_look_behind
predict_y_size = predict_ahead

### Data regression 0a model ###
model0a.prepareData <- function(){
    ### Prepare data for regression ###

    ### Prepare general data matrix (often will be too big - take it into account
    ### in the future)


    data_col = matrix(0, nrow = train.sample_count, ncol = predict_x_size + predict_y_size)
    indexes = (as.integer((0:(predict_x_size-1) )/2) + 1)
    indexes_time = rep(1:predict_look_behind, (train.link_count))
    colnames(data_col) = c(paste("L", indexes, "T", indexes_time, sep=""), paste("Y",1:predict_ahead,sep=""))


    ### Format L1T1... L1TK  ...   LNT1 .. LNTK Y1 .. YW ####
    ### K - minutes behind, N - number of links, Y - minutes ahead predict ###

    ### Process individual samples ###
    for(i in 1:train.sample_count){ 
        train.samples[[i]]$x = train.samples[[i]]$x[(train.minutes_sample - predict_look_behind+1):train.minutes_sample,] # get specified time sample
        train.samples[[i]]$x = c(as.matrix(train.samples[[i]]$x)) 
        data_col[i,1:predict_x_size] = train.samples[[i]]$x

        train.samples[[i]]$y = as.matrix(train.samples[[i]]$y)  
        train.samples[[i]]$y = as.matrix(train.samples[[i]]$y[,predict_link_id]) # choose link to predict
        train.samples[[i]]$y = as.matrix(train.samples[[i]]$y[1:predict_ahead, ]) # choose look ahead interval

        data_col[i, (predict_x_size+1):ncol(data_col)] = c(train.samples[[i]]$y)
    }
    return(as.data.frame(data_col))
}

# model0a.data.1010 = model0a.prepareData()
# save(model0a.data.1010, file = "model0a.data.1010.RDa")


load("model0a.data.1010.RDa")
print(summary(model0a.data.1010))

model.x_params = 40 # poprawic jako liste
model.test_size = 1000 # poprawic

#for(i in 1:train.sample_count){
#    model0a.data[i, ]= as.numeric(model0a.data.1010[i,])
#
#    if(i%%100==0) print(i)
#}
#save(model0a.data, file="model0a.data.1010.RDa")


Y1 = model0a.data[,predict_x_size+1]

print("### Training regression model ###")
lm.model0a = lm(Y1 ~ ., data = model0a.data[,1:40])
#print(summary(lm.model0a))


linearPredict <- function(x, weights){
    return( sum(x*weights[2:length(weights)]) + weights[1])
}

predicted = rep(0, model.test_size)
for(i in 1:model.test_size){#train.sample_count){
    predicted[i] = linearPredict(model0a.data[i,1:model.x_params], lm.model0a$co)
}
print(predicted)
plot(1:model.test_size, Y1[1:model.test_size]-predicted)

