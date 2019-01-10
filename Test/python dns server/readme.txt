Python Test Server Readme

the purpose of this folder is to get a working small local dns server to test the client functions.

INSTALL
	1) install python 3.
		https://www.python.org/
		or with local packet manager.
	2) install dnslib.
		on shell or commandline:
		pip install dnslib

RUN
	start shell or command line where python is available.

	py -m dnslib.zoneresolver --zone zone.txt --port 8053
		this will start a small dns server with the zone defined in the txt file.

TEST if server is running
	connect with your favorite dns client OR

	py -m dnslib.client --server 127.0.0.1:8053 www.example.com