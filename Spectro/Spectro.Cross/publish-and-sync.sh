sh publish-linux-arm.sh
rsync -av builds/linux-arm/ pi@raspberrypi.local:/home/pi/Spectro.Cross
