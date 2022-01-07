cloud chia plotter: fast chia plotting without SSD disk or large amount of memory.

It does not require SSD disk or large amount of memory, and can run well on low configuration rig, such as raspberry pi.

[https://anyplots.com/](https://anyplots.com) The marketplace to buy and sell chia plots, starts at $0.20/plot.

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
-b  set your real network bandwidth/speed(Mbps), such as:
    for 500Mbps:    -b 500            , for 1000Mbps:    -b 1000  
    please test your network speed at https://speedtest.net
<br/>               
for example:

.\\CloudChiaPlotter.exe -p 000005e13f5fc456753081edf7dcc98986dcffa15 -d  d:\\;e:\\;f:\\ -b 500

./CloudChiaPlotter -p 000005e13f5fc456753081edf7dcc98986dcffa15 -d  /mnt/d/;/mnt/e/;/mnt/f/ -b 500

You can run multiple processes at the same time.

</pre>

<h3>Tips</h3>
<h4>1.      Do not set parameters that are inconsistent with your actual bandwidth. If you set -b 1000 for the actual bandwidth of 500mbps, it will reduce your download speed in most cases, because the probability of network packet loss will increase.</h4>
<h4>2.      For bandwidth greater than 500mbps, two processes can be run at the same time, but the bandwidth parameter should be divided by 2.</h4>
<h4>3.      Under the condition that the program does not change the directory configuration, for uncompleted plot, It will continue to transmit the remaining part only.</h4>



<h3>for Windows.</h3>
<pre>
#Run it on Windows
#Now, open a powershell window (Click Start, type PowerShell, and then click Windows PowerShell)
#run the follow command(If there is a problem with the execution sequence, please execute line by line)

#del .\\CloudChiaPlotter.exe

Invoke-WebRequest -Uri  https://github.com/anyplots/cloud-chia-plotter/releases/download/v3/cloud-chia-plotter-v3-win-x64.exe -Outfile .\\CloudChiaPlotter.exe
.\\CloudChiaPlotter.exe -p {your project token} -d  d:\\;e:\\;f:\\ -b {your bandwidth}
</pre>


<h3>for Linux series.</h3>
<pre>
# run the follow script in the linux console
# Note that different platforms correspond to different link versions

#rm ./CloudChiaPlotter

wget -O CloudChiaPlotter https://github.com/anyplots/cloud-chia-plotter/releases/download/v3/cloud-chia-plotter-v3-linux-x64
chmod +x ./CloudChiaPlotter
./CloudChiaPlotter -p {your project token} -d  /mnt/d/;/mnt/e/;/mnt/f/ -b {your bandwidth}
</pre>


<h3>for Mac OS series.</h3>
<pre>
#Running the CloudChiaPlotter on Mac OS
#Now, open a terminal window (Mac OS version of the command line)
#run the follow command:

#rm ./CloudChiaPlotter
curl -O CloudChiaPlotter https://github.com/anyplots/cloud-chia-plotter/releases/download/v3/cloud-chia-plotter-v3-osx-x64
chmod +x ./CloudChiaPlotter
./CloudChiaPlotter -p {your project token} -d  /mnt/d/;/mnt/e/;/mnt/f/ -b {your bandwidth}
</pre>


<h3>for Arm64 Linux series(Raspberry Pi 3, Pi 4, etc)</h3>
<pre>
# run the follow script in the linux console
# Note that different platforms correspond to different link versions

#rm ./CloudChiaPlotter

wget -O CloudChiaPlotter https://github.com/anyplots/cloud-chia-plotter/releases/download/v3/cloud-chia-plotter-v3-linux-arm64
chmod +x ./CloudChiaPlotter
./CloudChiaPlotter -p {your project token} -d  /mnt/d/;/mnt/e/;/mnt/f/ -b {your bandwidth}
</pre>


<h3>for Arm32 Linux  series.</h3>
<pre>
# run the follow script in the linux console
# Note that different platforms correspond to different link versions

#rm ./CloudChiaPlotter

wget -O CloudChiaPlotter https://github.com/anyplots/cloud-chia-plotter/releases/download/v3/cloud-chia-plotter-v3-linux-arm32
chmod +x ./CloudChiaPlotter
./CloudChiaPlotter -p {your project token} -d  /mnt/d/;/mnt/e/;/mnt/f/ -b {your bandwidth}
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
