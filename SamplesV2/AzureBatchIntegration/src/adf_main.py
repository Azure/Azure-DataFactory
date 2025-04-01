import sys
import asyncio
import uuid
import tempfile
import decompressor

# constants
SRC_CONTAINER = "raw"
DEST_CONTAINER = "curated"

async def main():

    local_temp_dir = "{}/{}".format(tempfile.gettempdir(), uuid.uuid4()) 

    # storage account connection string is passed in as param
    conn_str = sys.argv[1]
    print("storage account connection string: " + conn_str)    
    
    de = decompressor.Decompressor(conn_str)
    await de.process_blob_files(SRC_CONTAINER, DEST_CONTAINER, local_temp_dir)
    
if __name__ == "__main__":
    asyncio.run(main())