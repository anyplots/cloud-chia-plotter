cloud chia plotter: fast chia plotting without SSD disk or large amount of memory.

It does not require SSD disk or large amount of memory, and can run well on low configuration rig, such as raspberry pi.

https://anyplots.com The marketplace to buy and sell chia plots, starts at $0.20/plot.

<h2>Cloud Chia Plotter Usage</h2>

<h3>Arguments:</h3>
<pre>
-p  project token ( get it from your project page https://anyplots.com/buy-chia-plot/projects ), such as(40 chars):
        -p 000005e13f5fc456753081edf7dcc98986dcffa15 

<br/>
-d  directories for save plots, separate multiple directories with semicolons, such as:
        -d  /mnt/d/;/mnt/e/;/mnt/f/
        -d  d:\;e:\;f:\
<br/>               
for example:

.\\CloudChiaPlotter.exe -p 000005e13f5fc456753081edf7dcc98986dcffa15 -d  d:\\;e:\\;f:\\

./CloudChiaPlotter -p 000005e13f5fc456753081edf7dcc98986dcffa15 -d  /mnt/d/;/mnt/e/;/mnt/f/

You can run multiple processes at the same time.

</pre>

<h3>for Linux series.</h3>
<pre>
# run the follow script in the linux console
# Note that different platforms correspond to different link versions

#rm ./CloudChiaPlotter

wget -O CloudChiaPlotter https://github.com/anyplots/cloud-chia-plotter/releases/download/v2/cloud-chia-plotter-v2-linux-x64
chmod +x ./CloudChiaPlotter
./CloudChiaPlotter -p {your project token} -d  /mnt/d/;/mnt/e/;/mnt/f/
</pre>

<h3>for Windows.</h3>
<pre>
#Run it on Windows
#Now, open a powershell window (Click Start, type PowerShell, and then click Windows PowerShell)
#run the follow command(If there is a problem with the execution sequence, please execute line by line)

#del .\\CloudChiaPlotter.exe

Invoke-WebRequest -Uri  https://github.com/anyplots/cloud-chia-plotter/releases/download/v2/cloud-chia-plotter-v2-win-x64.exe -Outfile .\\CloudChiaPlotter.exe
.\\CloudChiaPlotter.exe -p {your project token} -d  d:\\;e:\\;f:\\
</pre>


<h3>Tested download Speed</h3>
Different user experiences may vary, depending on your network and the plotting server you choose.
<pre>
download from U.S. based plotting server at the following countries(bandwidth 1 Gbit/s):
US: ping latency ~50ms,  download speed > 1000Mbit/s, ~ 13 minutes per plot.
UK: ping latency ~80ms,  download speed > 1000Mbit/s. ~ 13 minutes per plot.    
DE: ping latency ~100ms, download speed > 1000Mbit/s. ~ 13 minutes per plot.    
FR: ping latency ~100ms, download speed > 1000Mbit/s. ~ 13 minutes per plot.    
JP: ping latency ~160ms, download speed > 900Mbit/s.  ~ 15 minutes per plot.  
AU: ping latency ~220ms, download speed > 800Mbit/s.  ~ 16 minutes per plot.  
IN: ping latency ~280ms, download speed > 700Mbit/s.  ~ 18 minutes per plot.  




</pre>
