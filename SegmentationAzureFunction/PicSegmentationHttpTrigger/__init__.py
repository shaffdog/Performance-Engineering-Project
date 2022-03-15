import io
import logging
import os
import pixellib 
import PIL.Image as Image
import psutil
import time
import uuid

from pathlib import Path
from pixellib.torchbackend.instance import instanceSegmentation
from azure.cosmos import CosmosClient, PartitionKey, exceptions

import azure.functions as func


def main(req: func.HttpRequest,
        context: func.Context) -> func.HttpResponse:
    #Setting up metrics 
    start_time = time.time()
    load1, load5, load15 = psutil.getloadavg()
    id = str(uuid.uuid4())

    #Setting up CosmosDB connection
    #Enter URI and Key here fro CosmosDB
    cosmosURI = ""
    cosmosKey = ""
    client = CosmosClient(cosmosURI, credential=cosmosKey)
    database_name = 'TestDatabase'
    try:
        database = client.create_database(database_name)
    except exceptions.CosmosResourceExistsError:
        database = client.get_database_client(database_name)
    container_name = 'TestContainerAzureFunctionJobs'
    try:
        container = database.create_container(id=container_name, partition_key=PartitionKey(path="/productName"))
    except exceptions.CosmosResourceExistsError:
        container = database.get_container_client(container_name)
    except exceptions.CosmosHttpResponseError:
        raise

    #Parsing Input Image and setting up paths for input and output images
    logging.info('Python HTTP trigger function processed a request.')
    picByteArray = req.get_body()        
    inputImage = Image.open(io.BytesIO(picByteArray))    

    imageName = "%s.jpg"% id
    inputPath = Path(context.function_directory, '/inputImages\\')
    outputPath = Path(context.function_directory, '/outputImages\\')
    inputPath.mkdir(parents=True, exist_ok=True)
    outputPath.mkdir(parents=True, exist_ok=True)
    inputPathString = str(inputPath)+'\\'+imageName
    outputPathString = str(outputPath)+'\\'+imageName
    inputImage.save(inputPathString, "JPEG")

    
    #Setting up Model for pic segmentation and segmenting image
    ins = instanceSegmentation()
    ins.load_model("pointrend_resnet50.pkl")
    results, output = ins.segmentImage(inputPathString, show_bboxes=True, output_image_name=outputPathString)

    with open(outputPathString, "rb") as fh:
        outputByteArray = bytearray(fh.read())

    end_time = time.time()
    #total_memory, used_memory, free_memory = map(int, os.popen('free -t -m').readlines()[-1].split()[1:])
    time_lapsed = end_time - start_time
    #time_lapsed = time_convert(time_lapsed)
    cpu_usage1 = (load1/os.cpu_count()) * 100
    cpu_usage2 = psutil.cpu_percent(60)
    ram_usage1 = psutil.virtual_memory()[2]
    #ram_usage2 = round((used_memory/total_memory) * 100, 2)

    container.upsert_item(
        {
            'name': 'Instance_Segmentation_Function',
            'Job_Id': id,
            'start_time': start_time,
            'end_time': end_time,
            'time_lapsed': time_lapsed,
            'cpu_usage1': cpu_usage1,
            'cpu_usage2': cpu_usage2,
            'ram_usage1': ram_usage1,
            #'ram_usage2': ram_usage2,
            #'total_memory': total_memory,
            #'used_memory': used_memory,
            #'free_memory': free_memory
        }
    )

    
    return func.HttpResponse(                       
             body = outputByteArray,
             status_code=200,
             mimetype="image/jpeg"
        )