# AudioData C#
## Features
* Converting String into Audio.
* Hamming codes that supports 8 or 4 bits.
* Converting string into 8 bit arrays.
* Converting 8 bit arrays into a string.
* Handshakes to determine where data starts. (is kinda crucial for doing it cross devices)

## FSK
The FSK algoritm is really robust.
i've got a noise level generator that basically adds amplitude to the input audio and tests it.

this is how the curve of successrate looks like:
![FSK GRAPH](bin/Debug/net8.0/FSK/FSK_Trendline.png)

## QPSK

The QPSK algoritm works really well.

The slight problem i have that both happens here and in FSK is that sometimes when it switches from 0 -> 1 or 0 -> 1 it plays both frequencies at the same time. 

i will need to look into it.

![QPSK GRAPH](bin/Debug/net8.0/QPSK/QPSK_Trend.png)

# TLDR
It has worked with radios and to play teh data to other devices. 
It would be cool to see this work across alot of computers at the same time to send one-way data.

## Created by David Hornemark, 2024
