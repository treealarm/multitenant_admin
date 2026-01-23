#kubectl delete namespace myrealm

kubectl apply -f myrealm

kubectl rollout restart deployment -n myrealm


#kubectl get pods -n myrealm
#kubectl get svc  -n myrealm

#to see ip of the minikuber
#kubectl config view --minify