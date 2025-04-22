import logging
import time
import sys
import json

import asyncio
import tarfile
import os
import errno
import shutil
from pathlib import Path
from aiofile import async_open
from azure.storage.blob.aio import BlobServiceClient

class Decompressor():

    def __init__(self, conn_str):
        self.conn_str = conn_str

    async def process_blob_files(self, src_container, dest_container, local_temp_dir):
        try:
            self.init(local_temp_dir)

            # Create the BlobServiceClient object which will be used to create a container client
            blob_service_client = BlobServiceClient.from_connection_string(self.conn_str)           
            async with blob_service_client:
                container_client = blob_service_client.get_container_client(src_container)

                tasks = []
                async for blob in container_client.list_blobs():                    
                    blob_client = container_client.get_blob_client(blob.name)
                    local_file = "{}/{}".format(local_temp_dir, os.path.basename(blob.name))
                    tasks.append(self.process_blob_file(blob_client, dest_container, local_file))
                                    
                await asyncio.gather(*tasks)

            self.cleanup(local_temp_dir)
        except Exception as e:
            print(repr(e))
            sys.exit(1)


    async def process_blob_file(self, blob_client, dest_container, local_file):
        download_start = time.time()
        logging.info(f"start downloading file: {local_file}")
        await self.download_compressed_file(blob_client, local_file)
        download_end = time.time()
        logging.info(f"end downloading file: {local_file}, takes {download_end - download_start}")

        decompress_start = time.time()
        logging.info(f"start decompressing file: {local_file}")
        decompression_dir = self.decompress(local_file)
        decompress_end = time.time()
        logging.info(f"end decompressing file: {local_file}, takes {decompress_end - decompress_start}")
        
        upload_start = time.time()
        logging.info(f"start uploading files for: {local_file}")

        properties = await blob_client.get_blob_properties()
        metadata = properties.metadata
        metadata_json = json.dumps(metadata)

        await self.upload_decompressed_files(dest_container, decompression_dir, metadata_json)
        upload_end = time.time()
        logging.info(f"end uploading files for: {local_file}, takes {upload_end - upload_start}")

    async def download_compressed_file(self, blob_client, local_file):
        async with async_open(local_file, 'wb') as afp:
            download_stream = await blob_client.download_blob()
            await afp.write(await download_stream.readall())

    def get_files_to_upload(self, decompression_dir):
        files = []
        # r=root, d=directories, f = files
        for r, d, f in os.walk(decompression_dir):
            for file in f:
                files.append(os.path.join(r, file))

        return files

    async def upload_decompressed_files(self, dest_container, decompression_dir, metadata_json):
        blob_service_client = BlobServiceClient.from_connection_string(self.conn_str)

        files_to_upload = self.get_files_to_upload(decompression_dir)

        async with blob_service_client:
            for file in files_to_upload:
                await self.upload_decompressed_file(blob_service_client, file, dest_container, metadata_json)      

    async def upload_decompressed_file(self, blob_service_client, file, dest_container, metadata_json):
        blob_name = os.path.basename(file)
        
        blob_client = blob_service_client.get_blob_client(container=dest_container, blob=blob_name)

        async with async_open(file, "rb") as afp:
            await blob_client.upload_blob(await afp.read(), overwrite=True)     

    def decompress(self, downloaded_file):   
        decompression_dir = os.path.splitext(downloaded_file)[0]

        # decompress
        tar = tarfile.open(downloaded_file)
        tar.extractall(decompression_dir)
        tar.close()  

        return decompression_dir 

    # setup temporary directory for process
    def init(self, local_temp_dir):
        if os.path.exists(local_temp_dir):
            shutil.rmtree(local_temp_dir)
        
        try:
            os.makedirs(local_temp_dir)
        except OSError as exc: # Guard against race condition
            if exc.errno != errno.EEXIST:
                raise      

    def cleanup(self, local_temp_dir):
        shutil.rmtree(local_temp_dir)