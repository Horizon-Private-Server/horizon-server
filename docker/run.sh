docker run -it \
    --rm \
    -p 10075:10075/tcp \
    -p 10077:10077/tcp \
    -p 10078:10078/tcp \
    --name horizon-server \
    horizon-server
