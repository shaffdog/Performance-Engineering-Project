import io
import logging
import os
import uuid
import pixellib 
import PIL.Image as Image
from pathlib import Path
from pixellib.torchbackend.instance import instanceSegmentation

import azure.functions as func


def main(req: func.HttpRequest,
        context: func.Context) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    picByteArray = req.get_body()    

    inputImage = Image.open(io.BytesIO(picByteArray))    
    id = str(uuid.uuid4())
    imageName = "%s.jpg"% id
    inputPath = Path(context.function_directory, '/inputImages\\')
    outputPath = Path(context.function_directory, '/outputImages\\')
    pklPath= Path(context.function_directory, '/pklFolder\\')
    inputPath.mkdir(parents=True, exist_ok=True)
    outputPath.mkdir(parents=True, exist_ok=True)
    inputPathString = str(inputPath)+'\\'+imageName
    outputPathString = str(outputPath)+'\\'+imageName
    inputImage.save(inputPathString, "JPEG")

    ins = instanceSegmentation()
    ins.load_model("pointrend_resnet50.pkl")
    results, output = ins.segmentImage(inputPathString, show_bboxes=True, output_image_name=outputPathString)

    return func.HttpResponse(
             "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.",
             status_code=200
        )

    # name = req.params.get('name')
    # if not name:
    #     try:
    #         req_body = req.get_json()
    #     except ValueError:
    #         pass
    #     else:
    #         name = req_body.get('name')

    # if name:
    #     return func.HttpResponse(f"Hello, {name}. This HTTP triggered function executed successfully.")
    # else:
    #     return func.HttpResponse(
    #          "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response.",
    #          status_code=200
    #     )
