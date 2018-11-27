#region CopyRight 2018
/*
    Copyright (c) 2007-2018 Andreas Rohleder (andreas@rohleder.cc)
    All rights reserved
*/
#endregion
#region License LGPL-3
/*
    This program/library/sourcecode is free software; you can redistribute it
    and/or modify it under the terms of the GNU Lesser General Public License
    version 3 as published by the Free Software Foundation subsequent called
    the License.

    You may not use this program/library/sourcecode except in compliance
    with the License. The License is included in the LICENSE file
    found at the installation directory or the distribution package.

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be included
    in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion
#region Authors & Contributors
/*
   Author:
     Andreas Rohleder <andreas@rohleder.cc>

   Contributors:
 */
#endregion

namespace Cave.Net
{
    /// <summary>
    /// http://www.iana.org/assignments/protocol-numbers/protocol-numbers.xhtml
    /// </summary>
    public enum IanaProtocolNumber : byte
    {
        /// <summary>
        /// IPv6 Hop by Hop Option [RFC2460]
        /// </summary>
        HOPOPT = 0,

        /// <summary>
        /// Internet Control Message [RFC792]
        /// </summary>
        ICMP,

        /// <summary>
        /// Internet Group Management [RFC1112]
        /// </summary>
        IGMP,

        /// <summary>
        /// Gateway-to-Gateway [RFC823]
        /// </summary>
        GGP,

        /// <summary>
        /// IPv4 encapsulation [RFC2003]
        /// </summary>
        IPv4,

        /// <summary>
        /// Stream [RFC1190][RFC1819]
        /// </summary>
        ST,

        /// <summary>
        /// Transmission Control [RFC793]
        /// </summary>
        TCP,

        /// <summary>
        /// CBT [Tony_Ballardie]
        /// </summary>
        CBT,

        /// <summary>
        /// Exterior Gateway Protocol [RFC888][David_Mills]
        /// </summary>
        EGP,

        /// <summary>
        /// any private interior gateway (used by Cisco for their IGRP) [Internet_Assigned_Numbers_Authority]
        /// </summary>
        IGP,

        /// <summary>
        /// BBN RCC Monitoring [Steve_Chipman]
        /// </summary>
        BBN_RCC_MON,

        /// <summary>
        /// Network Voice Protocol [RFC741][Steve_Casner]
        /// </summary>
        NVP_II,

        /// <summary>
        /// [Boggs, D., J. Shoch, E. Taft, and R. Metcalfe, "PUP: An Internetwork Architecture", XEROX Palo Alto Research Center, CSL-79-10, July 1979; also in IEEE Transactions on Communication, Volume COM-28, Number 4, April 1980.][[XEROX]]
        /// </summary>
        PUP,

        /// <summary>
        /// ARGUS [Robert_W_Scheifler]
        /// </summary>
        ARGUS,

        /// <summary>
        /// Emission Control protocol
        /// </summary>
        EMCON,

        /// <summary>
        /// Cross Net Debugger [Haverty, J., "XNET Formats for Internet Protocol Version 4", IEN 158, October 1980.][Jack_Haverty]
        /// </summary>
        XNET,

        /// <summary>
        /// Chaos [J_Noechiappa]
        /// </summary>
        CHAOS,

        /// <summary>
        /// User Datagram [RFC768][Jon_Postel]
        /// </summary>
        UDP,

        /// <summary>
        /// Multiplexing [Cohen, D. and J. Postel, "Multiplexing Protocol", IEN 90, USC/Information Sciences Institute, May 1979.][Jon_Postel]
        /// </summary>
        MUX,

        /// <summary>
        /// DCN Measurement Subsystems [David_Mills]
        /// </summary>
        DCN_MEAS,

        /// <summary>
        /// Host Monitoring [RFC869][Bob_Hinden]
        /// </summary>
        HMP,

        /// <summary>
        /// Packet Radio Measurement [Zaw_Sing_Su]
        /// </summary>
        PRM,

        /// <summary>
        /// XEROX NS IDP ["The Ethernet, A Local Area Network: Data Link Layer and Physical Layer Specification", AA-K759B-TK, Digital Equipment Corporation, Maynard, MA. Also as: "The Ethernet - A Local Area Network", Version 1.0, Digital Equipment Corporation, Intel Corporation, Xerox Corporation, September 1980. And: "The Ethernet, A Local Area Network: Data Link Layer and Physical Layer Specifications", Digital, Intel and Xerox, November 1982. And: XEROX, "The Ethernet, A Local Area Network: Data Link Layer and Physical Layer Specification", X3T51/80-50, Xerox Corporation, Stamford, CT., October 1980.][[XEROX]]
        /// </summary>
        XNS_IDP,

        /// <summary>
        /// Trunk-1 [Barry_Boehm]
        /// </summary>
        TRUNK_1,

        /// <summary>
        /// Trunk-2 [Barry_Boehm]
        /// </summary>
        TRUNK_2,

        /// <summary>
        /// Leaf-1 [Barry_Boehm]
        /// </summary>
        LEAF_1,

        /// <summary>
        /// Leaf-2 [Barry_Boehm]
        /// </summary>
        LEAF_2,

        /// <summary>
        /// Reliable Data Protocol [RFC908][Bob_Hinden]
        /// </summary>
        RDP,

        /// <summary>
        /// Internet Reliable Transaction [RFC938][Trudy_Miller]
        /// </summary>
        IRTP,

        /// <summary>
        /// ISO Transport Protocol Class 4 [RFC905]
        /// </summary>
        ISO_TP4,

        /// <summary>
        /// Bulk Data Transfer Protocol [RFC969][David_Clark]
        /// </summary>
        NETBLT,

        /// <summary>
        /// MFE Network Services Protocol [Shuttleworth, B., "A Documentary of MFENet, a National Computer Network", UCRL-52317, Lawrence Livermore Labs, Livermore, California, June 1977.][Barry_Howard]
        /// </summary>
        MFE_NSP,

        /// <summary>
        /// MERIT Internodal Protocol [Hans_Werner_Braun]
        /// </summary>
        MERIT_INP,

        /// <summary>
        /// Datagram Congestion Control Protocol [RFC4340]
        /// </summary>
        DCCP,

        /// <summary>
        /// Third Party Connect Protocol [Stuart_A_Friedberg]
        /// </summary>
        _3PC,

        /// <summary>
        /// Inter-Domain Policy Routing Protocol [Martha_Steenstrup]
        /// </summary>
        IDPR,

        /// <summary>
        /// XTP [Greg_Chesson]
        /// </summary>
        XTP,

        /// <summary>
        /// Datagram Delivery Protocol [Wesley_Craig]
        /// </summary>
        DDP,

        /// <summary>
        /// IDPR Control Message Transport Proto [Martha_Steenstrup]
        /// </summary>
        IDPR_CMTP,

        /// <summary>
        /// TP++ Transport Protocol [Dirk_Fromhein]
        /// </summary>
        TP_PlusPlus,

        /// <summary>
        /// IL Transport Protocol [Dave_Presotto]
        /// </summary>
        IL,

        /// <summary>
        /// IPv6 encapsulation [RFC2473]
        /// </summary>
        IPv6,

        /// <summary>
        /// Source Demand Routing Protocol [Deborah_Estrin]
        /// </summary>
        SDRP,

        /// <summary>
        /// Routing Header for IPv6 [Steve_Deering]
        /// </summary>
        IPv6_Route,

        /// <summary>
        /// Fragment Header for IPv6 [Steve_Deering]
        /// </summary>
        IPv6_Frag,

        /// <summary>
        /// Inter-Domain Routing Protocol [Sue_Hares]
        /// </summary>
        IDRP,

        /// <summary>
        /// Reservation Protocol [RFC2205][RFC3209][Bob_Braden]
        /// </summary>
        RSVP,

        /// <summary>
        /// Generic Routing Encapsulation [RFC2784][Tony_Li]
        /// </summary>
        GRE,

        /// <summary>
        /// Dynamic Source Routing Protocol [RFC4728]
        /// </summary>
        DSR,

        /// <summary>
        /// BNA [Gary Salamon]
        /// </summary>
        BNA,

        /// <summary>
        /// Encap Security Payload [RFC4303]
        /// </summary>
        ESP,

        /// <summary>
        /// Authentication Header [RFC4302]
        /// </summary>
        AH,

        /// <summary>
        /// Integrated Net Layer Security TUBA [K_Robert_Glenn]
        /// </summary>
        I_NLSP,

        /// <summary>
        /// IP with Encryption [John_Ioannidis]
        /// </summary>
        SWIPE,

        /// <summary>
        /// NBMA Address Resolution Protocol [RFC1735]
        /// </summary>
        NARP,

        /// <summary>
        /// IP Mobility [Charlie_Perkins]
        /// </summary>
        MOBILE,

        /// <summary>
        /// Transport Layer Security Protocol using Kryptonet key management [Christer_Oberg]
        /// </summary>
        TLSP,

        /// <summary>
        /// SKIP [Tom_Markson]
        /// </summary>
        SKIP,

        /// <summary>
        /// ICMP for IPv6 [RFC2460]
        /// </summary>
        IPv6_ICMP,

        /// <summary>
        /// No Next Header for IPv6 [RFC2460]
        /// </summary>
        IPv6_NoNxt,

        /// <summary>
        /// Destination Options for IPv6 [RFC2460]
        /// </summary>
        IPv6_Opts,

        /// <summary>
        /// any host internal protocol [Internet_Assigned_Numbers_Authority]
        /// </summary>
        HostInternal,

        /// <summary>
        /// CFTP [Forsdick, H., "CFTP", Network Message, Bolt Beranek and Newman, January 1982.][Harry_Forsdick]
        /// </summary>
        CFTP,

        /// <summary>
        /// any local network [Internet_Assigned_Numbers_Authority]
        /// </summary>
        LocalNetwork,

        /// <summary>
        /// SATNET and Backroom EXPAK [Steven_Blumenthal]
        /// </summary>
        SAT_EXPAK,

        /// <summary>
        /// Kryptolan [Paul Liu]
        /// </summary>
        KRYPTOLAN,

        /// <summary>
        /// MIT Remote Virtual Disk Protocol [Michael_Greenwald]
        /// </summary>
        RVD,

        /// <summary>
        /// Internet Pluribus Packet Core [Steven_Blumenthal]
        /// </summary>
        IPPC,

        /// <summary>
        /// any distributed file system [Internet_Assigned_Numbers_Authority]
        /// </summary>
        DistributedFileSystem,

        /// <summary>
        /// SATNET Monitoring [Steven_Blumenthal]
        /// </summary>
        SAT_MON,

        /// <summary>
        /// VISA Protocol [Gene_Tsudik]
        /// </summary>
        VISA,

        /// <summary>
        /// Internet Packet Core Utility [Steven_Blumenthal]
        /// </summary>
        IPCV,

        /// <summary>
        /// Computer Protocol Network Executive [David Mittnacht]
        /// </summary>
        CPNX,

        /// <summary>
        /// Computer Protocol Heart Beat [David Mittnacht]
        /// </summary>
        CPHB,

        /// <summary>
        /// Wang Span Network [Victor Dafoulas]
        /// </summary>
        WSN,

        /// <summary>
        /// Packet Video Protocol [Steve_Casner]
        /// </summary>
        PVP,

        /// <summary>
        /// Backroom SATNET Monitoring [Steven_Blumenthal]
        /// </summary>
        BR_SAT_MON,

        /// <summary>
        /// SUN ND PROTOCOL-Temporary [William_Melohn]
        /// </summary>
        SUN_ND,

        /// <summary>
        /// WIDEBAND Monitoring [Steven_Blumenthal]
        /// </summary>
        WB_MON,

        /// <summary>
        /// WIDEBAND EXPAK [Steven_Blumenthal]
        /// </summary>
        WB_EXPAK,

        /// <summary>
        /// ISO Internet Protocol [Marshall_T_Rose]
        /// </summary>
        ISO_IP,

        /// <summary>
        /// VMTP [Dave_Cheriton]
        /// </summary>
        VMTP,

        /// <summary>
        /// SECURE-VMTP [Dave_Cheriton]
        /// </summary>
        SECURE_VMTP,

        /// <summary>
        /// VINES [Brian Horn]
        /// </summary>
        VINES,

        /// <summary>
        /// Transaction Transport Protocol [Jim_Stevens]
        /// </summary>
        TTP,

        /// <summary>
        /// Internet Protocol Traffic Manager [Jim_Stevens]
        /// </summary>
        IPTM,

        /// <summary>
        /// NSFNET-IGP [Hans_Werner_Braun]
        /// </summary>
        NSFNET_IGP,

        /// <summary>
        /// Dissimilar Gateway Protocol [M/A-COM Government Systems, "Dissimilar Gateway Protocol Specification, Draft Version", Contract no. CS901145, November 16, 1987.][Mike_Little]
        /// </summary>
        DGP,

        /// <summary>
        /// TCF [Guillermo_A_Loyola]
        /// </summary>
        TCF,

        /// <summary>
        /// EIGRP [Cisco Systems, "Gateway Server Reference Manual", Manual Revision B, January 10, 1988.][Guenther_Schreiner]
        /// </summary>
        EIGRP,

        /// <summary>
        /// OSPFIGP [RFC1583][RFC2328][RFC5340][John_Moy]
        /// </summary>
        OSPFIGP,

        /// <summary>
        /// Sprite RPC Protocol [Welch, B., "The Sprite Remote Procedure Call System", Technical Report, UCB/Computer Science Dept., 86/302, University of California at Berkeley, June 1986.][Bruce Willins]
        /// </summary>
        Sprite_RPC,

        /// <summary>
        /// Locus Address Resolution Protocol [Brian Horn]
        /// </summary>
        LARP,

        /// <summary>
        /// Multicast Transport Protocol [Susie_Armstrong]
        /// </summary>
        MTP,

        /// <summary>
        /// AX.25 Frames [Brian_Kantor]
        /// </summary>
        AX_25,

        /// <summary>
        /// IP-within-IP Encapsulation Protocol [John_Ioannidis]
        /// </summary>
        IPIP,

        /// <summary>
        /// Mobile Internetworking Control Pro. [John_Ioannidis]
        /// </summary>
        MICP,

        /// <summary>
        /// Semaphore Communications Sec. Pro. [Howard_Hart]
        /// </summary>
        SCC_SP,

        /// <summary>
        /// Ethernet-within-IP Encapsulation [RFC3378]
        /// </summary>
        ETHERIP,

        /// <summary>
        /// Encapsulation Header [RFC1241][Robert_Woodburn]
        /// </summary>
        ENCAP,

        /// <summary>
        /// any private encryption scheme [Internet_Assigned_Numbers_Authority]
        /// </summary>
        PrivateEncryption,

        /// <summary>
        /// GMTP [[RXB5]]
        /// </summary>
        GMTP,

        /// <summary>
        /// Ipsilon Flow Management Protocol [Bob_Hinden][November 1995, 1997.]
        /// </summary>
        IFMP,

        /// <summary>
        /// PNNI over IP [Ross_Callon]
        /// </summary>
        PNNI,

        /// <summary>
        /// Protocol Independent Multicast [RFC4601][Dino_Farinacci]
        /// </summary>
        PIM,

        /// <summary>
        /// ARIS [Nancy_Feldman]
        /// </summary>
        ARIS,

        /// <summary>
        /// SCPS [Robert_Durst]
        /// </summary>
        SCPS,

        /// <summary>
        /// QNX [Michael_Hunter]
        /// </summary>
        QNX,

        /// <summary>
        /// Active Networks [Bob_Braden]
        /// </summary>
        A_N,

        /// <summary>
        /// IP Payload Compression Protocol [RFC2393]
        /// </summary>
        IPComp,

        /// <summary>
        /// Sitara Networks Protocol [Manickam_R_Sridhar]
        /// </summary>
        SNP,

        /// <summary>
        /// Compaq Peer Protocol [Victor_Volpe]
        /// </summary>
        Compaq_Peer,

        /// <summary>
        /// IPX in IP [CJ_Lee]
        /// </summary>
        IPX_in_IP,

        /// <summary>
        /// Virtual Router Redundancy Protocol [RFC5798]
        /// </summary>
        VRRP,

        /// <summary>
        /// PGM Reliable Transport Protocol [Tony_Speakman]
        /// </summary>
        PGM,

        /// <summary>
        /// any 0-hop protocol [Internet_Assigned_Numbers_Authority]
        /// </summary>
        ZeroHopProtocol,

        /// <summary>
        /// Layer Two Tunneling Protocol [RFC3931][Bernard_Aboba]
        /// </summary>
        L2TP,

        /// <summary>
        /// D-II Data Exchange (DDX) [John_Worley]
        /// </summary>
        DDX,

        /// <summary>
        /// Interactive Agent Transfer Protocol [John_Murphy]
        /// </summary>
        IATP,

        /// <summary>
        /// Schedule Transfer Protocol [Jean_Michel_Pittet]
        /// </summary>
        STP,

        /// <summary>
        /// SpectraLink Radio Protocol [Mark_Hamilton]
        /// </summary>
        SRP,

        /// <summary>
        /// UTI [Peter_Lothberg]
        /// </summary>
        UTI,

        /// <summary>
        /// Simple Message Protocol [Leif_Ekblad]
        /// </summary>
        SMP,

        /// <summary>
        /// Simple Multicast Protocol [Jon_Crowcroft][draft-perlman-simple-multicast]
        /// </summary>
        SM,

        /// <summary>
        /// Performance Transparency Protocol [Michael_Welzl]
        /// </summary>
        PTP,

        /// <summary>
        /// ISIS over IPv4 [Tony_Przygienda]
        /// </summary>
        ISIS_over_IPv4,

        /// <summary>
        /// [Criag_Partridge]
        /// </summary>
        FIRE,

        /// <summary>
        /// Combat Radio Transport Protocol [Robert_Sautter]
        /// </summary>
        CRTP,

        /// <summary>
        /// Combat Radio User Datagram [Robert_Sautter]
        /// </summary>
        CRUDP,

        /// <summary>
        /// [Kurt_Waber]
        /// </summary>
        SSCOPMCE,

        /// <summary>
        /// [[Hollbach]]
        /// </summary>
        IPLT,

        /// <summary>
        /// Secure Packet Shield [Bill_McIntosh]
        /// </summary>
        SPS,

        /// <summary>
        /// Private IP Encapsulation within IP [Bernhard_Petri]
        /// </summary>
        PIPE,

        /// <summary>
        /// Stream Control Transmission Protocol [Randall_R_Stewart]
        /// </summary>
        SCTP,

        /// <summary>
        /// Fibre Channel [Murali_Rajagopal][RFC6172]
        /// </summary>
        FC,

        /// <summary>
        /// [RFC3175]
        /// </summary>
        RSVP_E2E_IGNORE,

        /// <summary>
        /// [RFC6275]
        /// </summary>
        Mobility_Header,

        /// <summary>
        /// [RFC3828]
        /// </summary>
        UDPLite,

        /// <summary>
        /// [RFC4023]
        /// </summary>
        MPLS_in_IP,

        /// <summary>
        /// MANET Protocols [RFC5498]
        /// </summary>
        MANET,

        /// <summary>
        /// Host Identity Protocol [RFC-ietf-hip-rfc5201-bis-20]
        /// </summary>
        HIP,

        /// <summary>
        /// Shim6 Protocol [RFC5533]
        /// </summary>
        Shim6,

        /// <summary>
        /// Wrapped Encapsulating Security Payload [RFC5840]
        /// </summary>
        WESP,

        /// <summary>
        /// Robust Header Compression [RFC5858]
        /// </summary>
        ROHC,

        /// <summary>
        /// Use for experimentation and testing [RFC3692]
        /// </summary>
        Experimental1 = 253,

        /// <summary>
        /// Use for experimentation and testing [RFC3692]
        /// </summary>
        Experimental2 = 254,

        /// <summary>
        /// Will not be assigned
        /// </summary>
        Reserved = 255,
    }
}
