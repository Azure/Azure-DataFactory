# Summarize the image

## Request payload
payload = {
    "messages": [
    {
        "role": "system",
        "content": [
        {
            "type": "text",
            "text": "You are an AI assistant that helps an Azure engineer understand an image that likely shows a Data Factory in Microsoft Fabric data pipeline. Show list of pipeline activities and how they are connected."
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

response_json = response.json()

## Show AI response
print(response_json["choices"][0]['message']['content'])
