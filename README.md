YoutubeToMp3
============
This is my first github repo, if you see weird things popping up, that's me just experimenting with 
stuff.
Anyways, this is a YouTube to mp3 converter I wrote. The UI has the main focus here, I like good looking applications.
Only the audio is downloaded, no bandwidth is wasted on downloading the video you're discarding during conversion.
The .mp4 is converted to .mp3 using ffmpeg.

#Features
- Nice UI
- Only downloads audio, no video
- Converts to mp3 with VBR up to 320kbps using ffmpeg
- Threaded & async

#Todo
- Let the user select a format and bitrate (right now it just picks the highest quality available)
- Let the user select a directory to save the file in (right now it just downloads to your desktop)
- Automatic id3 tagging?
