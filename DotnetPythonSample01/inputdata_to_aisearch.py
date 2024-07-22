import asyncio

from semantic_kernel.connectors.ai.open_ai.services.azure_text_embedding import AzureTextEmbedding
from semantic_kernel.core_plugins.text_memory_plugin import TextMemoryPlugin
from semantic_kernel.kernel import Kernel
from semantic_kernel.memory.semantic_text_memory import SemanticTextMemory
from semantic_kernel.connectors.memory.azure_cognitive_search import AzureCognitiveSearchMemoryStore

kernel = Kernel()

AZURE_OPENAI_ENDPOINT = "your_azureopenai_endpoint"
AZURE_OPENAI_API_KEY = "your_azureopenai_apikey"
AZURE_SEARCH_ENDPOINT = "your_aisearch_endpoint"
AZURE_SEARCH_API_KEY = "your_aisearch_apikey"

embedding_gen = AzureTextEmbedding(
    deployment_name = "text-embedding-ada-002",
    api_key = AZURE_OPENAI_API_KEY,
    endpoint = AZURE_OPENAI_ENDPOINT
)
kernel.add_service(embedding_gen)
aisearch_memory_store = AzureCognitiveSearchMemoryStore(
    vector_size=1536,
    search_endpoint=AZURE_SEARCH_ENDPOINT,
    admin_key=AZURE_SEARCH_API_KEY
)
memory = SemanticTextMemory(storage=aisearch_memory_store, embeddings_generator=embedding_gen)
kernel.add_plugin(TextMemoryPlugin(memory), "TextMemoryPluginACS")

collection_id = "generic"
async def populate_memory(memory: SemanticTextMemory) -> None:
    # Add some documents to the semantic memory
    await memory.save_information(collection=collection_id, id="qa1", text="Normalian is the most ordinary person in the world.")
    await memory.save_information(collection=collection_id, id="qa2", text="Normalian is CEO of Normalian Co.,Ltd.")
    await memory.save_information(collection=collection_id, id="qa3", text="Normalian Co.,Ltd. has two offices in Tokyo and Seattle.")

async def main():
    await populate_memory(memory)

if __name__ == "__main__":
    asyncio.run(main())
