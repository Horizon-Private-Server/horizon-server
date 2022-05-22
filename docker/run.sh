docker run -it \
    --rm \
    -p 10071:10071/tcp \
    -p 10075:10075/tcp \
    -p 10077:10077/tcp \
    -p 10078:10078/tcp \
    -p 10073:10073/tcp \
    -p 50000:50000/udp \
    -p 10070:10070/udp \
    --name horizon-server \
    horizon-server