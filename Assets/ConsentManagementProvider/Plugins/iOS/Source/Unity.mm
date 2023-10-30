//  Unity.mm
//  Created by Vilas Mane and Dmytro Fedko on 12/04/21.

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <WebKit/WebKit.h>
#import "ConsentViewController-Swift.h"
#import "UnityPlugin-Bridging-Header.h"

static SwiftBridge * swiftBridge = nil;
typedef void (*СallbackCharMessage) (const char*);

extern "C"
{
    void _setUnityCallback (const char * gameObjectName){
        //
    }

    void _setCallbackDefault (СallbackCharMessage callback){
        if (swiftBridge == nil)
            swiftBridge = [[SwiftBridge alloc] init];
        [swiftBridge setCallbackDefaultWithCallback:callback];
    }

    void _setCallbackOnConsentReady (СallbackCharMessage callback){
        if (swiftBridge == nil)
            swiftBridge = [[SwiftBridge alloc] init];
        [swiftBridge setCallbackOnConsentReadyWithCallback:callback];
    }

    void _setCallbackOnConsentUIReady (СallbackCharMessage callback){
        if (swiftBridge == nil)
            swiftBridge = [[SwiftBridge alloc] init];
        [swiftBridge setCallbackOnConsentUIReadyWithCallback:callback];
    }

    void _setCallbackOnConsentAction (СallbackCharMessage callback){
        if (swiftBridge == nil)
            swiftBridge = [[SwiftBridge alloc] init];
        [swiftBridge setCallbackOnConsentActionWithCallback:callback];
    }

    void _setCallbackOnConsentUIFinished (СallbackCharMessage callback){
        if (swiftBridge == nil)
            swiftBridge = [[SwiftBridge alloc] init];
        [swiftBridge setCallbackOnConsentUIFinishedWithCallback:callback];
    }

    void _setCallbackOnErrorCallback (СallbackCharMessage callback){
        if (swiftBridge == nil)
            swiftBridge = [[SwiftBridge alloc] init];
        [swiftBridge setCallbackOnErrorCallbackWithCallback:callback];
    }

    void _setCallbackOnSPFinished (СallbackCharMessage callback){
        if (swiftBridge == nil)
            swiftBridge = [[SwiftBridge alloc] init];
        [swiftBridge setCallbackOnSPFinishedWithCallback:callback];
    }

    void _initLib()
    {
        if (swiftBridge == nil)
            swiftBridge = [[SwiftBridge alloc] init];
    }

    void _configLib(int accountId, int propertyId, char* propertyName, bool gdpr, bool ccpa, SPMessageLanguage language, char* gdprPmId, char* ccpaPmId)
    {
        [swiftBridge configLibWithAccountId:accountId propertyId:propertyId propertyName:[NSString stringWithFormat:@"%s", propertyName] gdpr:gdpr ccpa:ccpa language:language gdprPmId:[NSString stringWithFormat:@"%s", gdprPmId] ccpaPmId:[NSString stringWithFormat:@"%s", ccpaPmId]];
    }

    void _loadMessage(char * authId)
    {
        [swiftBridge loadMessage];
    }

    void _loadGDPRPrivacyManager()
    {
        [swiftBridge onGDPRPrivacyManagerTap];
    }

    void _loadCCPAPrivacyManager()
    {
        [swiftBridge onCCPAPrivacyManagerTap];
    }

    void _cleanConsent()
    {
        [swiftBridge onClearConsentTap];
    }
    
    void _customConsentGDPRWithVendors()
    {
        //
    }
}
