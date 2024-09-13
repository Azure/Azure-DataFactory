# Analyze the image and generate the pipeline JSON

## Setup new payload
payload = {
    "messages": [
    {
        "role": "system",
        "content": [
        {
            "type": "text",
            "text": "You are an AI assistant that helps an Azure engineer understand an image that likely shows a Data Factory in Microsoft Fabric data pipeline. Succeeded is denoted by a green line, and Fail is denoted by a red line. Generate an ADF v2 pipeline JSON with what you see. Return ONLY the JSON text required, without any leading or trailing markdown denoting a code block."
        }
        ]
    },
    {
        "role": "user",
        "content": [
        {
            "type": "image_url",
            "image_url": {
            "url": f"data:image/jpeg;base64,{encoded_image}"
            }
        }
        ]
    }
    ],
    "temperature": 0.7,
    "top_p": 0.95,
    "max_tokens": 800
}

## Send request
try:
    response = requests.post(AZURE_OPENAI_GPT4O_ENDPOINT, headers=headers, json=payload)
    response.raise_for_status()  # Will raise an HTTPError if the HTTP request returned an unsuccessful status code
except requests.RequestException as e:
    raise SystemExit(f"Failed to make the request. Error: {e}")

## Get JSON from request and show
response_json = response.json()
pipeline_json = response_json["choices"][0]['message']['content']
print(pipeline_json)
