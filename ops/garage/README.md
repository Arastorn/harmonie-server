# Garage Local Setup

This repository uses `Garage` as the local S3-compatible object storage backend for uploads.

## Start Garage

```bash
docker compose up -d garage
```

## Initialize the single-node layout

Get the node identifier:

```bash
docker exec harmonie-garage /garage -c /etc/garage.toml status
```

Assign the node to a zone and apply the layout:

```bash
docker exec harmonie-garage /garage -c /etc/garage.toml layout assign <node-id> -z local -c 10G
docker exec harmonie-garage /garage -c /etc/garage.toml layout apply --version 1
```

## Create the uploads bucket

```bash
docker exec harmonie-garage /garage -c /etc/garage.toml bucket create harmonie-uploads
```

## Create an S3 key and grant access

Create a key:

```bash
docker exec harmonie-garage /garage -c /etc/garage.toml key new --name harmonie-uploads
```

Use the printed access key ID and secret access key in `ObjectStorage__AccessKeyId` and
`ObjectStorage__SecretAccessKey`, then grant access to the bucket:

```bash
docker exec harmonie-garage /garage -c /etc/garage.toml bucket allow harmonie-uploads --read --write --owner --key harmonie-uploads
```

## API configuration

Set these values for the API:

- `ObjectStorage__Endpoint=http://garage:3900`
- `ObjectStorage__PublicBaseUrl=http://localhost:3900/harmonie-uploads`
- `ObjectStorage__BucketName=harmonie-uploads`
- `ObjectStorage__Region=garage`
- `ObjectStorage__AccessKeyId=<garage access key id>`
- `ObjectStorage__SecretAccessKey=<garage secret access key>`
