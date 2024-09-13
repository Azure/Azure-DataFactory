# Configuration
AZURE_OPENAI_KEY = "<Your Azure OpenAI key>"
AZURE_OPENAI_GPT4O_ENDPOINT = "<Your Azure OpenAI gpt-4o deployment endpoint>"
IMAGE_PATH = "<Path to your uploaded image file>" # For example, "/lakehouse/default/files/images/pipeline.png"

# Install the OpenAI library
!pip install semantic-link --q 
!pip uninstall --yes openai
!pip install openai
%pip install openai --upgrade

# Imports
import os
import requests
import base64
import json
import time
import pprint
import openai
import sempy.fabric as fabric
import pandas as pd

# Load the image
image_bytes = open(IMAGE_PATH, 'rb').read()
encoded_image = base64.b64encode(image_bytes).decode('ascii')

## Request headers
headers = {
    "Content-Type": "application/json",
    "api-key": AZURE_OPENAI_KEY,
}
