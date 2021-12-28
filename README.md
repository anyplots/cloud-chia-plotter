cloud chia plotter: fast chia plotting without SSD disk or large amount of memory.

It does not require SSD disk or large amount of memory, and can run well on low configuration rig, such as raspberry pie

https://anyplots.com The marketplace to buy and sell chia plots

<h2>Cloud Chia Plotter Usage</h2>

<h3>Arguments:</h3>
<pre>
-p  project token ( get it from your project page https://anyplots.com/buy-chia-plot/projects ), such as(40 chars):
    -p 000005e13f5fc456753081edf7dcc98986dcffa15 
-d  directories for save plots, separate multiple directories with semicolons, such as:
    -d  /mnt/d/;/mnt/e/;/mnt/f/
    -d  d:\;e:\;f:\
You can run multiple processes at the same time.
</pre>

<h3>for Linux series.</h3>
<pre>
# run the follow script in the linux console
# Note that different platforms correspond to different link versions
#rm ./CloudChiaPlotter
wget -O CloudChiaPlotter https://github.com/anyplots/cloud-chia-plotter/releases/download/v1/cloud-chia-plotter-v1-linux-x64
chmod +x ./CloudChiaPlotter
./CloudChiaPlotter -p {your project token} -d  /mnt/d/;/mnt/e/;/mnt/f/
</pre>

<h3>for Windows.</h3>
<pre>
#Run it on Windows
#Now, open a powershell window (Click Start, type PowerShell, and then click Windows PowerShell)
#run the follow command(If there is a problem with the execution sequence, please execute line by line)

#del ./CloudChiaPlotter.exe
Invoke-WebRequest -Uri  https://github.com/anyplots/cloud-chia-plotter/releases/download/v1/cloud-chia-plotter-v1-win-x64.exe -Outfile ./CloudChiaPlotter.exe
./CloudChiaPlotter.exe -p {your project token} -d  d:\;e:\;f:\
</pre>


<h3>Tested download Speed</h3>
Different user experiences may vary, depending on your network and the plotting server you choose.
<pre>
download from U.S. based plotting server at the following countries(bandwidth 1 Gbit/s):
US > 1000Mbit/s
DE > 1000Mbit/s
UK > 1000Mbit/s
FR > 1000Mbit/s
JP > 900Mbit/s
AU > 800Mbit/s
IN > 700Mbit/s



</pre>
