kubectl delete namespace infra

kubectl apply -f infra


kubectl get pods -n infra
kubectl get svc  -n infra

#to see ip of the minikuber
#kubectl config view --minify