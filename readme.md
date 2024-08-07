# Local AI Dictation (and pass to local LLM)

This project is designed to process voice using the Whisper AI model from OpenAI and then process LLM requests using the Mistral 7B model using Ollama API.

## Description

This project is a combination of two powerful AI models. First, we use the Whisper AI model from OpenAI to process voice data. Whisper is an automatic speech recognition (ASR) system that has been trained on a large amount of multilingual and multitask supervised data collected from the web.

After the voice data has been processed, we then use the Mistral model from the Ollama API to process LLM requests. Mistral is a powerful model that is capable of handling a wide range of tasks.

Since Mistral 7B has a massive 8K Token context, we also prepend the prompt passed in to ground the model with useful grounding data.

## Environment Variables

The application uses the following **optional** environment variables:

- `WHISPER_SERVER_IP`: The IP address of the Whisper server. Defaults to "localhost".
- `WHISPER_AI_INITIAL_PROMPT_PATH`: Path to the file containing the initial prompt for the Whisper AI.
- `WHISPER_AI_POST_PROCESSING_PATH`: Path to the file containing post-processing instructions for the Whisper AI.
- `OLLAMA_SERVER_IP`: The IP address of the Ollama server. Defaults to "localhost".
- `OLLAMA_MODEL`: The model to use with the Ollama API. Defaults to "phi3".
- `AI_PROMPT_DICTATION`: Path to the file containing the dictation prompt for the AI.
- `AI_PROMPT_ACT_AS_ME`: Path to the file containing the "act as me" prompt for the AI.
- `AI_BASE_CONTEXT`: Path to the file containing the base context for the AI.

## Getting Started

### Dependencies

This is designed to run in WSL for Windows on a PC with NVIDIA GPU *(confirmed working on a 6GB NVIDIA GPU)*.

Install NVIDIA CUDA software and WSL driver:

* <https://developer.nvidia.com/cuda-downloads>
* <https://developer.nvidia.com/cuda/wsl>

Run Wisper locally in docker:

* https://github.com/ahmetoner/whisper-asr-webservice

e.g. `docker run -d --gpus all -p 9000:9000 -e ASR_MODEL=base -e ASR_ENGINE=openai_whisper onerahmet/openai-whisper-asr-webservice:latest-gpu`

Run Mistral locally in Ollama (mistral 7B "mini" with 8K context):

* `ollama serve`
* `ollama run mistral` - run once to install the model

Ollama api server has a 5 minute timeout to releases resources which is [not currently user configurable](https://github.com/jmorganca/ollama/issues/837#issuecomment-1771354540). If you want to keep it alive, you can periodically send an empty generate request:

`while :; do curl localhost:11434/api/generate -d '{"model":"mistral","system":"","prompt":"","template":""}'; sleep 60; done`

### Executing program

Ensure whisper and mistral are running. Run the program and follow the prompts.
