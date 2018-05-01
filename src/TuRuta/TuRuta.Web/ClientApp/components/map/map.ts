﻿import Vue from 'vue'
import { Component, Prop } from 'vue-property-decorator'
import { } from "@types/googlemaps"
import PubNub from 'pubnub';

interface Message {
    latitude: number;
    longitude: number;
    busId: string;
    nextStop: stopVM;
}

@Component
export default class MapComponent extends Vue {

    // Data
    pubnub?: PubNub;
    map?: google.maps.Map = undefined;
    city: string = '';
    markers: google.maps.Marker[] = [];
    poly: any = null;
    busesTable: { [id:string] : google.maps.Marker } = {};
    stopMarker?: google.maps.Marker;

    // Properties
    @Prop() stops?: stopVM[];
    @Prop() buses?: busVM[];
    @Prop() isMarkerActive?: boolean;

    // Private props
    private listener = {
        status: function (statusEvent: any) {
            if (statusEvent.category === "PNConnectedCategory") {
                console.log("Conectado");
            }
        },
        message: this.messageReceived
    }

    // Lifecycle
    mounted() {
        this.initMap()
        if(this.stops != undefined && this.stops.length != 0) {
            this.iniMarkers()
        }

        if (this.buses != undefined && this.buses.length != 0) {
            this.initPubnubListeners();
        }
    }

    beforeDestroy() {
        if(this.pubnub != undefined){
            this.pubnub.unsubscribeAll();
            this.pubnub.removeListener(this.listener);
        }
    }

    // Methods
    messageReceived(message: any) {
        var busUpdate = message.message;
        let latLon = new google.maps.LatLng(busUpdate.Location.Latitude, busUpdate.Location.Longitude);
        this.busesTable[message.message.BusId].setPosition(latLon);
    }

    initPubnubListeners() {
        this.fetchPubNub()
            .then(() => {
                var busIds = new Array<string>();
                this.buses!.forEach(bus => {
                    busIds.push(bus.id);
                });

                this.pubnub!.subscribe({
                    channels: busIds
                });
            })

        for (let bus of this.buses!) {
            let marker = this.createMarker(bus.location, bus.licensePlate, true);
            this.busesTable[bus.id] = marker;
        }
    }

    contentString:string = "<p>hello</p>";

    createMarker(location: point, title: string, isBus: boolean): google.maps.Marker {
        let contentInfo = new google.maps.InfoWindow({
            content: this.contentString
        });

        let latLon = new google.maps.LatLng(location.latitude, location.longitude);
        let marker = new google.maps.Marker({
            position: latLon,
            map: this.map,
            title: title
        });
        marker.addListener("click", () => contentInfo.open(this.map, marker));

        if (isBus) {
            marker.setIcon({
                path: google.maps.SymbolPath.CIRCLE,
                scale: 10
            });
        }

        return marker;
    }

    initMap(){
        let element = document.getElementById("mapDiv")
        let latLon = new google.maps.LatLng(20.6736, -103.344)
        let options = {
            zoom: 14,
            center: latLon,
            mapTypeId: google.maps.MapTypeId.ROADMAP
        };
        this.map = new google.maps.Map(element, options);

        this.map.addListener("click", this.addClickMarker)
    }

    addClickMarker(args: any){
        if (this.$props.isMarkerActive) {
            if(this.stopMarker != undefined){
                this.stopMarker.setMap(null);
            }
            var latLon = args.latLng as google.maps.LatLng;
            this.stopMarker = this.createMarker({latitude: latLon.lat(), longitude: latLon.lng()}, "", false);
            this.$emit("addMarker", {latitude: latLon.lat(), longitude: latLon.lng()});
        }
    }

    
    iniMarkers() {
        var newMarkers = new Array<google.maps.Marker>()
        if (this.stops != undefined && this.stops.length != 0) {
            this.stops.forEach(stop => {
                let marker = this.createMarker(stop.location, stop.name, false);

                newMarkers.push(marker);
            });
            this.markers = newMarkers;
            var centerPosition = Math.floor(this.markers.length / 2);
            this.map!.setCenter(this.markers[centerPosition].getPosition());
        }
    }

    fetchPubNub() {
        return new Promise((resolve, error) => fetch('/api/config/pubnub')
            .then(response => response.json() as Promise<any>)
            .then(data => {
                this.pubnub = new PubNub({
                    subscribeKey: data.subKey,
                    ssl: true
                })
                this.pubnub.addListener(this.listener);
                resolve();
            })
        );
    }
}